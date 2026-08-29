using System;
using System.Collections.Generic;
using UnityEngine;

// Orchestrates the 7-phase round loop. Boss/Card systems and manual-phase UI hook into
// OnPhaseChanged, OnRoundChanged, OnActionLog, and CompleteManualPhase().
public class GameManager : MonoBehaviour
{
    [Serializable]
    private class PhaseInstructions
    {
        [TextArea] public string planning = "The boss chooses its attack for this round.";
        [TextArea] public string shield = "Restore the boss shields for this round.";
        [TextArea] public string action = "Players may scan and play their action cards.";
        [TextArea] public string attack = "Resolve the boss's planned attack.";
        [TextArea] public string status = "Resolve active status effects.";
        [TextArea] public string dropCards = "Discard any cards you do not want to keep.";
        [TextArea] public string drawCards = "Draw cards for the next round.";
    }

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
    private bool _resumeQrScanningWhenMessageCloses;
    private readonly HashSet<DamageType> _attackTypesPlayedThisRound = new HashSet<DamageType>();

    public bool HasActivePhasePopup { get; private set; }
    public GamePhase ActivePopupPhase { get; private set; }
    public string ActivePopupText { get; private set; }
    public bool HasActiveMessagePopup { get; private set; }
    public string ActiveMessageTitle { get; private set; }
    public string ActiveMessageText { get; private set; }

    public event Action<GamePhase> OnPhaseChanged;
    public event Action<int> OnRoundChanged;
    public event Action<string> OnActionLog;
    public event Action<Player> OnPlayerActionPerformed;
    public event Action<GamePhase, string> OnPhasePopupRequested;
    public event Action<string, string> OnMessagePopupRequested;

    public GamePhase CurrentPhase => _phaseStateMachine.CurrentPhase.PhaseId;
    public int Round => _phaseStateMachine.Round;
    public int PlayerCount => players.Count;
    public IReadOnlyList<Player> Players => players;
    public Player CurrentPlayer { get; private set; }
    public BossController Boss { get; private set; } = new BossController();

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

        Boss.OnHPChanged += (hp, maxHp) =>
        {
            Log($"Boss HP: {hp}/{maxHp}");
            if (Boss.IsDefeated)
                Log("Boss defeated! Game over.");
        };
        Boss.OnShieldDestroyed += type => Log($"Boss {type} shield destroyed!");
        Boss.OnBossAttackPlanned += attack => Log($"Boss attacks with '{attack.name}': {attack.damage} damage" +
            (attack.statusEffect != StatusEffectType.None ? $" + {attack.statusEffect}" : string.Empty));
        Boss.OnReactionTriggered += reaction => Log($"Boss reaction: players take {reaction.retaliationDamage} damage ({reaction.description})");
        Boss.OnHPTriggerFired += trigger => Log($"Boss HP trigger at {trigger.hpThreshold} HP: +{trigger.attackBonusDamage} attack damage ({trigger.description})");
        Boss.OnTimeTriggerFired += trigger => Log($"Boss time trigger (round {trigger.triggerOnRound}): players take {trigger.damageToAllPlayers} damage ({trigger.description})");
        Boss.OnShieldTriggerFired += trigger => Log($"Boss shield trigger ({trigger.shieldType}): players take {trigger.damageOnDestroy} damage ({trigger.description})");

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
            _attackTypesPlayedThisRound.Clear();

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
        }

        OnPhaseChanged?.Invoke(phase.PhaseId);
    }

    private void HandleRoundChanged(int newRound)
    {
        // newRound is already incremented, so the round that just finished is newRound - 1.
        if (newRound > 1)
            Boss.EvaluateTimeTriggers(newRound - 1);

        OnRoundChanged?.Invoke(newRound);
    }

    private void Start()
    {
        _phaseStateMachine.Start(this);
    }

    private void Update()
    {
        if (!HasActiveMessagePopup)
            _phaseStateMachine.Tick(this);
    }

    // Called by UI (e.g. a "Next Phase" button) to end the current DropCards/DrawCards phase.
    public void CompleteManualPhase()
    {
        if (_phaseStateMachine.CurrentPhase is PopupPhase popupPhase)
            popupPhase.Complete();
        else
            Log($"CompleteManualPhase() ignored: {CurrentPhase} must finish through its own game logic.");
    }

    public void ShowPhasePopup(GamePhase phase, string instructionText)
    {
        HasActivePhasePopup = true;
        ActivePopupPhase = phase;
        ActivePopupText = instructionText;
        OnPhasePopupRequested?.Invoke(phase, instructionText);
    }

    public void DismissPhasePopup()
    {
        HasActivePhasePopup = false;

        if (CurrentPhase != GamePhase.Action)
            CompleteManualPhase();
    }

    public void ShowMessagePopup(string title, string message)
    {
        HasActiveMessagePopup = true;
        ActiveMessageTitle = title;
        ActiveMessageText = message;
        OnMessagePopupRequested?.Invoke(title, message);
    }

    public void DismissMessagePopup()
    {
        HasActiveMessagePopup = false;

        if (_resumeQrScanningWhenMessageCloses && qrCodeReader != null)
        {
            qrCodeReader.SetScanningEnabled(true);
            _resumeQrScanningWhenMessageCloses = false;
        }
    }

    public void SetCurrentPlayer(Player player)
    {
        CurrentPlayer = player;
    }

    // Called once per scanned card (or a temporary "Use Action" button) to consume the current
    // player's action during the Action Phase and pass the turn to the next player.
    public void PerformPlayerAction()
    {
        if (_phaseStateMachine.CurrentPhase is ActionPhase actionPhase)
        {
            Player actingPlayer = CurrentPlayer;
            actionPhase.PerformAction(this);
            OnPlayerActionPerformed?.Invoke(actingPlayer);
        }
        else
            Log($"PerformPlayerAction() ignored: {CurrentPhase} is not the Action phase.");
    }

    public Player CurrentActionPlayer => CurrentPlayer;

    // Resolves a QR ID into a card and plays it only while actions are available.
    public bool UseCardByQrId(string qrId)
    {
        if (CurrentPhase != GamePhase.Action)
        {
            Log($"Card '{qrId}' ignored: cards can only be played during the Action phase.");
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

        ResolveCardEffects(card);
        PerformPlayerAction();
        ShowPlayedCardPopup(card);
        return true;
    }

    private void ResolveCardEffects(CardData card)
    {
        if (card.effects == null || card.effects.Count == 0)
        {
            Log($"Card '{card.cardName}' has no effects configured.");
            ShowMessagePopup("Card Cannot Be Played", $"'{card.cardName}' has no effects configured.");
            return;
        }

        foreach (CardEffect effect in card.effects)
        {
            switch (effect)
            {
                case AttackCardEffect attack:
                    ResolveAttackEffect(attack);
                    break;
                case SupportCardEffect support:
                    ResolveSupportEffect(support);
                    break;
                case ProtectionCardEffect protection:
                    ResolveProtectionEffect(protection);
                    break;
                case LightningCardEffect lightning:
                    ResolveLightningEffect(lightning);
                    break;
                case HealCardEffect heal:
                    ResolveHealEffect(heal);
                    break;
                case DrawCardEffect draw:
                    ResolveDrawEffect(draw);
                    break;
                case SpecialCardEffect special:
                    ResolveSpecialEffect(special);
                    break;
                case null:
                    Log($"Card '{card.cardName}' contains an empty effect entry.");
                    break;
            }
        }
    }

    private void ResolveAttackEffect(AttackCardEffect effect)
    {
        Boss.TakeDamage(effect.damage, effect.damageType);
        _attackTypesPlayedThisRound.Add(effect.damageType);
        Log($"Player {CurrentPlayer.PlayerNumber} used a {effect.damageType} attack for {effect.damage} damage.");
    }

    private void ResolveSupportEffect(SupportCardEffect effect)
    {
        if (!_attackTypesPlayedThisRound.Contains(effect.damageType))
        {
            ShowMessagePopup("Support Cannot Be Used", $"A {effect.damageType} attack must be played earlier this round.");
            return;
        }

        Boss.TakeDamage(effect.damage, effect.damageType);
        Log($"Player {CurrentPlayer.PlayerNumber} used a {effect.damageType} support for {effect.damage} damage.");
    }

    private void ResolveProtectionEffect(ProtectionCardEffect effect)
    {
        Log($"Protection effect: choose a player to reduce their next boss attack by {effect.protection}.");
    }

    private void ResolveLightningEffect(LightningCardEffect effect)
    {
        CurrentPlayer.AddActions(effect.additionalActions);
        Log($"Player {CurrentPlayer.PlayerNumber} gains {effect.additionalActions} additional action(s).");
    }

    private void ResolveHealEffect(HealCardEffect effect)
    {
        Log($"Heal effect: Player {CurrentPlayer.PlayerNumber} recovers {effect.healing} health.");
    }

    private void ResolveDrawEffect(DrawCardEffect effect)
    {
        Log($"Draw effect: Player {CurrentPlayer.PlayerNumber} draws {effect.cardsToDraw} card(s).");
    }

    private void ResolveSpecialEffect(SpecialCardEffect effect)
    {
        Log(string.IsNullOrWhiteSpace(effect.description) ? "Special effect resolved." : effect.description);
    }

    private void ShowPlayedCardPopup(CardData card)
    {
        if (qrCodeReader != null)
        {
            qrCodeReader.SetScanningEnabled(false);
            _resumeQrScanningWhenMessageCloses = true;
        }

        string message = string.IsNullOrWhiteSpace(card.description)
            ? $"{card.cardName} was played."
            : card.description;
        ShowMessagePopup(card.cardName, message);
    }

    private bool CanCurrentPlayerUseCard(CardData card)
    {
        bool matchingHero = card.heroType == HeroType.All || card.heroType == CurrentPlayer.HeroType;
        bool matchingClass = card.classType == ClassType.All || card.classType == CurrentPlayer.ClassType;
        return matchingHero && matchingClass;
    }

    public void Log(string message)
    {
        Debug.Log($"[GameManager] {message}");
        OnActionLog?.Invoke(message);
    }
}
