using System;
using System.Collections.Generic;
using UnityEngine;

// Orchestrates the 7-phase round loop: wires boss, cards, QR scanning, and dialogs
// together and exposes events the UI reacts to. Game rules live in the collaborators.
public class GameManager : MonoBehaviour
{
    [SerializeField] private int actionsPerPlayer = 3;
    [SerializeField] private List<Player> players = new List<Player>
    {
        new Player(1),
        new Player(2),
        new Player(3),
        new Player(4)
    };
    [SerializeField] private BossData bossData;
    [SerializeField] private CardDatabase cardDatabase;
    [SerializeField] private QRCodeReader qrCodeReader;
    [SerializeField] private PhaseInstructions phaseInstructions = new PhaseInstructions();

    private PhaseStateMachine _phaseStateMachine;
    private CardRegistry _cardRegistry;
    private CardEffectResolver _cardEffectResolver;
    private BossAbilitySystem _bossAbilitySystem;
    private readonly Queue<BossTrigger> _pendingBossTriggers = new Queue<BossTrigger>();
    private readonly Queue<ProtectionCardEffect> _pendingProtections = new Queue<ProtectionCardEffect>();
    private readonly Queue<ExtraTurnCardEffect> _pendingExtraTurns = new Queue<ExtraTurnCardEffect>();
    private readonly Queue<CleanseAttackCardEffect> _pendingCleanses = new Queue<CleanseAttackCardEffect>();
    private readonly Queue<ShieldStrikeCardEffect> _pendingShieldStrikes = new Queue<ShieldStrikeCardEffect>();
    private enum PlayerSelectionMode { None, ProtectionTarget, ExtraTurnTarget, StartPlayer, CleanseTarget, ShieldTarget }
    private PlayerSelectionMode _selectionMode = PlayerSelectionMode.None;
    private bool _isResolvingCard;

    public GameDialogs Dialogs { get; } = new GameDialogs();

    public event Action<GamePhase> OnPhaseChanged;
    public event Action<int> OnRoundChanged;
    public event Action<string> OnActionLog;
    public event Action<Player> OnPlayerActionPerformed;
    public event Action<Player> OnCurrentPlayerChanged;
    public event Action<DamageType, int> OnAttackEffectExecuted;

    public GamePhase CurrentPhase => _phaseStateMachine.CurrentPhase.PhaseId;
    public int Round => _phaseStateMachine.Round;
    public int PlayerCount => players.Count;
    public IReadOnlyList<Player> Players => players;
    public Player CurrentPlayer { get; private set; }
    public BossController Boss { get; private set; } = new BossController();

    // True while boss-trigger popups still need to be acknowledged before the phase continues.
    public bool HasPendingBossTriggers => _pendingBossTriggers.Count > 0;

    public bool WasAttackTypePlayedThisRound(DamageType type) =>
        _cardEffectResolver != null && _cardEffectResolver.WasAttackTypePlayedThisRound(type);

    private void Awake()
    {
        _cardRegistry = new CardRegistry(cardDatabase);

        if (qrCodeReader == null)
            qrCodeReader = FindAnyObjectByType<QRCodeReader>();

        if (qrCodeReader != null)
            qrCodeReader.OnQRCodeScanned += HandleQRCodeScanned;
        else
            Debug.LogWarning("[GameManager] No QRCodeReader found; cards can only be played through test controls.");

        if (bossData != null)
            Boss.Initialize(bossData);
        else
            Debug.LogWarning("[GameManager] No BossData assigned; boss will remain uninitialized.");

        Boss.SetPlayers(players);
        _bossAbilitySystem = new BossAbilitySystem(Boss, players);
        _bossAbilitySystem.OnTriggerDue += HandleBossTriggerDue;

        _cardEffectResolver = new CardEffectResolver(Boss, Log, ShowMessagePopup, RequestProtectionTarget, RequestExtraTurnTarget, RequestCleanseTarget, () => Round, RequestShieldTarget);
        _cardEffectResolver.OnAttackEffectExecuted += (type, amount) => OnAttackEffectExecuted?.Invoke(type, amount);
        _cardEffectResolver.OnCardResolved += HandleCardResolved;

        Boss.OnHPChanged += (hp, maxHp) =>
        {
            Log($"Boss HP: {hp}/{maxHp}");
            if (Boss.IsDefeated)
                Log("Boss defeated! Game over.");
        };
        Boss.OnShieldDestroyed += type => Log($"Boss {type} shield destroyed!");
        Boss.OnPoisonTokensChanged += tokens => Log($"Boss poison tokens: {tokens}");
        Boss.OnBossAttackPlanned += attack => Log($"Boss plans '{attack.Name}' on Player {attack.Target.PlayerNumber}: {attack.Damage} damage" +
            (attack.StatusEffect != StatusEffectType.None ? $" + {attack.StatusEffect}" : string.Empty));

        var phases = new List<IGamePhase>
        {
            new PlanningPhase(phaseInstructions.planning),
            new ShieldPhase(phaseInstructions.shield),
            new ActionPhase(players, actionsPerPlayer, phaseInstructions.action),
            new AttackPhase(phaseInstructions.attack),
            new StatusPhase(phaseInstructions.status),
            new DropCardsPhase(phaseInstructions.dropCards),
            new DrawCardsPhase(phaseInstructions.drawCards)
        };

        _phaseStateMachine = new PhaseStateMachine(phases);
        _phaseStateMachine.OnPhaseEntered += HandlePhaseEntered;
        _phaseStateMachine.OnRoundChanged += HandleRoundChanged;
    }

    private void OnDestroy()
    {
        if (qrCodeReader != null)
            qrCodeReader.OnQRCodeScanned -= HandleQRCodeScanned;
    }

    private void HandleQRCodeScanned(string qrId)
    {
        UseCardByQrId(qrId);
    }

    private void HandlePhaseEntered(IGamePhase phase)
    {
        if (phase.PhaseId == GamePhase.Action)
        {
            _cardEffectResolver.ResetRound();
            _bossAbilitySystem.ResetRound();
        }

        switch (phase.PhaseId)
        {
            case GamePhase.Planning:
                Boss.PlanAttacks();
                break;
            case GamePhase.Shield:
                Boss.ResetShields();
                break;
            case GamePhase.Attack:
                Boss.ExecutePlannedAttacks();
                break;
            case GamePhase.Status:
                Boss.TickPoison();
                break;
        }

        OnPhaseChanged?.Invoke(phase.PhaseId);

        // Modular boss triggers fire at phase entry, after the phase's own logic ran.
        _bossAbilitySystem.EvaluatePhaseEntry(phase.PhaseId, Round);
    }

    private void HandleBossTriggerDue(BossTrigger trigger)
    {
        _pendingBossTriggers.Enqueue(trigger);
        Log($"Boss trigger: {trigger.triggerName}");
        ShowNextBossTriggerPopup();
    }

    private void ShowNextBossTriggerPopup()
    {
        if (_pendingBossTriggers.Count == 0 || Dialogs.HasActiveMessagePopup)
            return;

        BossTrigger trigger = _pendingBossTriggers.Peek();
        ShowMessagePopup($"Boss: {trigger.triggerName}", BossAbilitySystem.DescribeTrigger(trigger));
    }

    private void HandleRoundChanged(int newRound)
    {
        OnRoundChanged?.Invoke(newRound);
    }

    private void Start()
    {
        _phaseStateMachine.Start(this);
    }

    private void Update()
    {
        if (!Dialogs.HasActiveMessagePopup)
            _phaseStateMachine.Tick(this);
    }

    // Called by UI (e.g. a "Next Phase" button) to end the current DropCards/DrawCards phase.
    public void CompleteManualPhase()
    {
        if (_phaseStateMachine.CurrentPhase is PopupPhase popupPhase)
            popupPhase.CompleteWhenIdle(this);
        else
            Log($"CompleteManualPhase() ignored: {CurrentPhase} must finish through its own game logic.");
    }

    public void ShowPhasePopup(GamePhase phase, string instructionText)
    {
        Dialogs.RequestPhasePopup(phase, instructionText);
    }

    public void DismissPhasePopup()
    {
        Dialogs.DismissPhasePopup();

        if (CurrentPhase != GamePhase.Action)
            CompleteManualPhase();
        else if (_phaseStateMachine.CurrentPhase is PopupPhase popupPhase)
            popupPhase.CompleteWhenIdle(this);
    }

    public void ShowMessagePopup(string title, string message)
    {
        Dialogs.RequestMessage(title, message);
        UpdateQrScanningState();
    }

    public void DismissMessagePopup()
    {
        Dialogs.DismissMessage();

        // A boss trigger popup was closed: apply its effects, then show the next one.
        if (_pendingBossTriggers.Count > 0)
        {
            BossTrigger trigger = _pendingBossTriggers.Dequeue();
            _bossAbilitySystem.ApplyEffects(trigger, Round, Log);

            if (_pendingBossTriggers.Count > 0)
            {
                ShowNextBossTriggerPopup();
                return;
            }

            // Queue drained. If it was opened standalone (shield break between cards,
            // not during a card), resume scanning here.
            if (!_isResolvingCard)
                UpdateQrScanningState();

            return;
        }

        bool startedSelection = false;
        if (_pendingProtections.Count > 0)
        {
            _selectionMode = PlayerSelectionMode.ProtectionTarget;
            Dialogs.BeginPlayerSelection();
            startedSelection = true;
        }
        else if (_pendingExtraTurns.Count > 0)
        {
            _selectionMode = PlayerSelectionMode.ExtraTurnTarget;
            Dialogs.BeginPlayerSelection();
            startedSelection = true;
        }
        else if (_pendingCleanses.Count > 0)
        {
            _selectionMode = PlayerSelectionMode.CleanseTarget;
            Dialogs.BeginPlayerSelection();
            startedSelection = true;
        }
        else if (_pendingShieldStrikes.Count > 0)
        {
            _selectionMode = PlayerSelectionMode.ShieldTarget;
            Dialogs.BeginShieldSelection();
            startedSelection = true;
        }

        // Non-selection effects resume the queue as soon as their popup closes;
        // selection effects resume after the player picks a target.
        if (!startedSelection)
            _cardEffectResolver.CompleteInteraction(CurrentPlayer);

        // A boss trigger that fired mid-card (shield break) is shown once the card's
        // queue has no pending popup: surface it now, before scanning resumes.
        if (!Dialogs.HasActiveMessagePopup && _bossAbilitySystem.HasDeferredTriggers)
        {
            _bossAbilitySystem.FlushQueuedTriggers();
            ShowNextBossTriggerPopup();
        }

        UpdateQrScanningState();
    }

    public void SetCurrentPlayer(Player player)
    {
        if (CurrentPlayer == player)
            return;

        CurrentPlayer = player;
        OnCurrentPlayerChanged?.Invoke(player);
    }

    private void RequestProtectionTarget(ProtectionCardEffect effect)
    {
        _pendingProtections.Enqueue(effect);
        Log($"Protection: select a player to reduce their boss attack by {effect.protection.Evaluate(Round, BossAttackAgainstCurrentPlayer())}.");
    }

    private int BossAttackAgainstCurrentPlayer()
    {
        PlannedBossAttack attack = CurrentPlayer == null ? null : Boss.GetPlannedAttack(CurrentPlayer);
        return attack?.Damage ?? 0;
    }

    private void RequestExtraTurnTarget(ExtraTurnCardEffect effect, Player preselected)
    {
        // A preselected target (PreviousTarget) skips the choice and grants the turn directly.
        if (preselected != null)
        {
            ApplyExtraTurnTarget(preselected);
            return;
        }

        _pendingExtraTurns.Enqueue(effect);
        Log("Extra Turn: choose a player to take an immediate turn.");
    }

    private void ApplyExtraTurnTarget(Player target)
    {
        Player sourcePlayer = CurrentPlayer;
        CurrentPlayer.SelectTarget(target);
        Log($"Extra Turn: Player {target.PlayerNumber} takes an immediate turn.");
        GrantInstantTurn(target, sourcePlayer);
        // The source's extra-turn effect stays open until the interruption finishes;
        // meanwhile the target may scan their card.
        _isResolvingCard = false;
        UpdateQrScanningState();
    }

    private void RequestCleanseTarget(CleanseAttackCardEffect effect)
    {
        _pendingCleanses.Enqueue(effect);
        Log("Cleanse: choose a player whose boss attack loses its status effect.");
    }

    private void RequestShieldTarget(ShieldStrikeCardEffect effect)
    {
        _pendingShieldStrikes.Enqueue(effect);
        Log($"Shield Strike: click a boss shield to reduce it by {effect.damage.Evaluate(Round, BossAttackAgainstCurrentPlayer())}.");
    }

    // Called by the HUD when a shield icon is clicked while ShieldTarget selection is active.
    public bool TrySelectShieldTarget(DamageType type)
    {
        if (_selectionMode != PlayerSelectionMode.ShieldTarget || _pendingShieldStrikes.Count == 0)
            return false;

        ShieldStrikeCardEffect effect = _pendingShieldStrikes.Dequeue();
        Dialogs.EndShieldSelection();
        _selectionMode = PlayerSelectionMode.None;
        int amount = effect.damage.Evaluate(Round, BossAttackAgainstCurrentPlayer());

        if (effect.suppressShieldTrigger)
            Boss.DisarmShieldTrigger(type);

        int poison = effect.poisonOnBreak.Evaluate(Round, BossAttackAgainstCurrentPlayer());
        if (poison > 0)
            Boss.ArmShieldBreakPoison(type, poison);

        Boss.DamageShield(type, amount);
        Log($"Shield Strike: {type} shield reduced by {amount} to {Boss.GetShield(type)}" +
            (effect.suppressShieldTrigger ? " (trigger suppressed)" : string.Empty) +
            (poison > 0 ? $" (armed with {poison} poison on break)" : string.Empty) + ".");
        _cardEffectResolver.CompleteInteraction(CurrentPlayer);
        return true;
    }

    public bool TrySelectProtectionTarget(Player target)
    {
        if (_pendingProtections.Count == 0 || target == null)
            return false;

        ProtectionCardEffect effect = _pendingProtections.Dequeue();
        Dialogs.EndPlayerSelection();
        _selectionMode = PlayerSelectionMode.None;
        CurrentPlayer.SelectTarget(target);
        int protectionAmount = effect.protection.Evaluate(Round, BossAttackAgainstCurrentPlayer());
        Boss.ReducePlayerAttackDamage(target, protectionAmount);
        Log($"Protection: Player {target.PlayerNumber}'s boss attack is reduced by {protectionAmount}.");
        _cardEffectResolver.CompleteInteraction(CurrentPlayer);
        return true;
    }

    public bool TrySelectExtraTurnTarget(Player target)
    {
        if (_pendingExtraTurns.Count == 0 || target == null)
            return false;

        _pendingExtraTurns.Dequeue();
        Dialogs.EndPlayerSelection();
        _selectionMode = PlayerSelectionMode.None;
        ApplyExtraTurnTarget(target);
        return true;
    }

    public bool TrySelectCleanseTarget(Player target)
    {
        if (_pendingCleanses.Count == 0 || target == null)
            return false;

        _pendingCleanses.Dequeue();
        Dialogs.EndPlayerSelection();
        _selectionMode = PlayerSelectionMode.None;
        CurrentPlayer.SelectTarget(target);
        Boss.RemovePlayerAttackStatusEffect(target);
        Log($"Cleanse: Player {target.PlayerNumber}'s boss attack loses its status effect.");
        _cardEffectResolver.CompleteInteraction(CurrentPlayer);
        return true;
    }

    public void HandlePlayerPanelClicked(Player player)
    {
        switch (_selectionMode)
        {
            case PlayerSelectionMode.ProtectionTarget: TrySelectProtectionTarget(player); break;
            case PlayerSelectionMode.ExtraTurnTarget: TrySelectExtraTurnTarget(player); break;
            case PlayerSelectionMode.CleanseTarget: TrySelectCleanseTarget(player); break;
            case PlayerSelectionMode.StartPlayer: TrySelectStartPlayer(player); break;
        }
    }

    // Action phase entry: reset per-round card/selection state, then wait for a player
    // panel click to choose the starting player.
    public void BeginActionPhase(string startPlayerPrompt)
    {
        _isResolvingCard = false;
        _pendingProtections.Clear();
        _pendingExtraTurns.Clear();
        _pendingCleanses.Clear();
        _pendingShieldStrikes.Clear();
        _selectionMode = PlayerSelectionMode.StartPlayer;
        Dialogs.IsStartPlayerSelectionActive = true;
        ShowPhasePopup(GamePhase.Action, startPlayerPrompt);
        Dialogs.BeginPlayerSelection();
        UpdateQrScanningState();
    }

    private void TrySelectStartPlayer(Player player)
    {
        if (player == null)
            return;

        _selectionMode = PlayerSelectionMode.None;
        Dialogs.IsStartPlayerSelectionActive = false;
        Dialogs.EndPlayerSelection();
        DismissPhasePopup();

        if (_phaseStateMachine.CurrentPhase is ActionPhase actionPhase)
            actionPhase.SetStartingPlayer(this, player);

        // Re-arm the one-shot reader; the start-player notification consumed its scan window.
        if (qrCodeReader != null)
            qrCodeReader.SetScanningEnabled(true);
    }

    // Called once per scanned card (or a temporary "Use Action" button) to consume the current
    // player's action during the Action Phase and pass the turn to the next player.
    public void PerformPlayerAction()
    {
        PerformPlayerAction(false);
    }

    public void PerformPlayerAction(bool grantExtraAction)
    {
        if (_phaseStateMachine.CurrentPhase is ActionPhase actionPhase)
        {
            Player actingPlayer = CurrentPlayer;
            actionPhase.PerformAction(this, grantExtraAction);
            OnPlayerActionPerformed?.Invoke(actingPlayer);
        }
        else
            Log($"PerformPlayerAction() ignored: {CurrentPhase} is not the Action phase.");
    }

    // Resolves a QR ID into a card and plays it only while actions are available.
    public bool UseCardByQrId(string qrId)
    {
        if (CurrentPhase != GamePhase.Action)
        {
            Log($"Card '{qrId}' ignored: cards can only be played during the Action phase.");
            return false;
        }

        if (_isResolvingCard)
        {
            Log($"Card '{qrId}' ignored: a card is still resolving.");
            return false;
        }

        // No active player until a start player is chosen; ignore scans until then.
        if (CurrentPlayer == null)
        {
            Log($"Card '{qrId}' ignored: no active player yet.");
            return false;
        }

        if (!_cardRegistry.TryGetCard(qrId, out CardData card))
        {
            Log($"No card registered for QR ID '{qrId}'.");
            ShowMessagePopup("Card Not Found", $"No card is registered for QR ID '{qrId}'.");
            return false;
        }

        if (!CanCurrentPlayerUseCard(card))
        {
            string message = $"Player {CurrentPlayer.PlayerNumber} is {CurrentPlayer.HeroType} {CurrentPlayer.ClassType}, " +
                $"but '{card.cardName}' requires {card.heroType} {card.classType}.";
            Log($"Card '{card.cardName}' cannot be played: {message}");
            ShowMessagePopup("Card Cannot Be Played", message);
            return false;
        }

        // Mark the card as resolving before Resolve: non-interactive cards fire
        // OnCardResolved (-> HandleCardResolved) synchronously inside Resolve.
        _isResolvingCard = true;
        UpdateQrScanningState();

        _cardEffectResolver.Resolve(CurrentPlayer, card);
        return true;
    }

    private void HandleCardResolved(Player player)
    {
        // An interruption target's card finished: consume their action, return control to
        // the source, and resume the source's paused card.
        if (_phaseStateMachine.CurrentPhase is ActionPhase actionPhase && actionPhase.IsInterruptionActive)
        {
            actionPhase.CompleteInterruption(this);
            _isResolvingCard = true;
            _cardEffectResolver.CompleteInteraction(CurrentPlayer);
            UpdateQrScanningState();
            return;
        }

        _isResolvingCard = false;
        PerformPlayerAction(player.ExtraActionsGrantedByCard > 0);

        // A shield broken during this card surfaces its boss trigger between cards.
        if (!Dialogs.HasActiveMessagePopup && _bossAbilitySystem.HasDeferredTriggers)
        {
            _bossAbilitySystem.FlushQueuedTriggers();
            ShowNextBossTriggerPopup();
        }

        UpdateQrScanningState();
    }

    private void UpdateQrScanningState()
    {
        if (qrCodeReader != null)
            qrCodeReader.SetScanningEnabled(!_isResolvingCard && !Dialogs.HasActiveMessagePopup);
    }

    private void GrantInstantTurn(Player target, Player sourcePlayer)
    {
        if (_phaseStateMachine.CurrentPhase is ActionPhase actionPhase)
            actionPhase.BeginInterruption(this, target, sourcePlayer);
        else
            SetCurrentPlayer(target);
    }

    private bool CanCurrentPlayerUseCard(CardData card)
    {
        // All works both ways: a card marked All fits every player, and a player marked
        // All can use every card regardless of its hero/class restriction.
        bool matchingHero = card.heroType == HeroType.All || CurrentPlayer.HeroType == HeroType.All
            || card.heroType == CurrentPlayer.HeroType;
        bool matchingClass = card.classType == ClassType.All || CurrentPlayer.ClassType == ClassType.All
            || card.classType == CurrentPlayer.ClassType;
        return matchingHero && matchingClass;
    }

    public void Log(string message)
    {
        Debug.Log($"[GameManager] {message}");
        OnActionLog?.Invoke(message);
    }
}
