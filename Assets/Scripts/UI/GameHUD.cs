using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// HUD: boss HP + shields plus one runtime-built panel per player. Subscribes to
// GameManager/Boss events and rebuilds lookups when PanelRenderer reloads its tree.
[RequireComponent(typeof(PanelRenderer))]
public class GameHUD : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    private PanelRenderer _panelRenderer;
    private int _uiVersion = -1;
    private Label _bossNameLabel;
    private VisualElement _hpFill;
    private Label _hpLabel;
    private Label _roundLabel;
    private Label _meleeShieldLabel;
    private Label _rangedShieldLabel;
    private Label _magicShieldLabel;
    private VisualElement _meleeShieldIcon;
    private VisualElement _rangedShieldIcon;
    private VisualElement _magicShieldIcon;
    private VisualElement _playerRow;
    private readonly List<PlayerPanelView> _playerPanels = new List<PlayerPanelView>();
    private TextField _cardIdInput;
    private Button _playCardButton;
    private VisualElement _phasePopup;
    private Label _phasePopupTitle;
    private Label _phasePopupMessage;
    private Button _closePhasePopupButton;
    private VisualElement _messagePopup;
    private Label _messagePopupTitle;
    private Label _messagePopupText;
    private Button _closeMessagePopupButton;

    private void Awake()
    {
        _panelRenderer = GetComponent<PanelRenderer>();

        if (_panelRenderer.panelSettings == null)
            _panelRenderer.panelSettings = ScriptableObject.CreateInstance<PanelSettings>();

        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

        if (gameManager == null)
            Debug.LogError("[GameHUD] No GameManager found in the scene; HUD will not update.");

        _panelRenderer.RegisterUIReloadCallback(OnUIReload);
    }

    private void OnDestroy()
    {
        _panelRenderer.UnregisterUIReloadCallback(OnUIReload);
    }

    private void OnUIReload(PanelRenderer panelRenderer, VisualElement root, int version)
    {
        if (_uiVersion == version)
            return;
        _uiVersion = version;

        _bossNameLabel = root.Q<Label>("boss-name");
        _hpFill = root.Q<VisualElement>("hp-fill");
        _hpLabel = root.Q<Label>("hp-label");
        _roundLabel = root.Q<Label>("round-label");
        _meleeShieldLabel = root.Q<Label>("melee-shield-label");
        _rangedShieldLabel = root.Q<Label>("ranged-shield-label");
        _magicShieldLabel = root.Q<Label>("magic-shield-label");
        _meleeShieldIcon = root.Q<VisualElement>("melee-shield-icon");
        _rangedShieldIcon = root.Q<VisualElement>("ranged-shield-icon");
        _magicShieldIcon = root.Q<VisualElement>("magic-shield-icon");
        _playerRow = root.Q<VisualElement>("player-row");
        _cardIdInput = root.Q<TextField>("card-id-input");
        _playCardButton = root.Q<Button>("play-card-button");
        _phasePopup = root.Q<VisualElement>("phase-popup");
        _phasePopupTitle = root.Q<Label>("phase-popup-title");
        _phasePopupMessage = root.Q<Label>("phase-popup-message");
        _closePhasePopupButton = root.Q<Button>("close-phase-popup-button");
        _messagePopup = root.Q<VisualElement>("message-popup");
        _messagePopupTitle = root.Q<Label>("message-popup-title");
        _messagePopupText = root.Q<Label>("message-popup-text");
        _closeMessagePopupButton = root.Q<Button>("close-message-popup-button");

        if (_playCardButton != null)
            _playCardButton.clicked += PlayTestCard;
        if (_closePhasePopupButton != null)
            _closePhasePopupButton.clicked += ClosePhasePopup;
        if (_closeMessagePopupButton != null)
            _closeMessagePopupButton.clicked += CloseMessagePopup;
        if (_cardIdInput != null)
            _cardIdInput.RegisterCallback<KeyDownEvent>(HandleCardIdKeyDown);

        BuildPlayerPanels();

        if (gameManager != null && gameManager.Dialogs.HasActivePhasePopup)
            ShowPhasePopup(gameManager.Dialogs.ActivePopupPhase, gameManager.Dialogs.ActivePopupText);
        if (gameManager != null && gameManager.Dialogs.HasActiveMessagePopup)
            ShowMessagePopup(gameManager.Dialogs.ActiveMessageTitle, gameManager.Dialogs.ActiveMessageText);

        RefreshAll();
    }

    private void OnEnable()
    {
        if (gameManager == null)
            return;

        gameManager.Boss.OnHPChanged += HandleHPChanged;
        gameManager.Boss.OnShieldChanged += HandleShieldChanged;
        gameManager.Boss.OnBossAttackPlanned += HandleBossAttackPlanned;
        gameManager.Boss.OnPlayerAttackDamageChanged += HandlePlayerAttackDamageChanged;
        gameManager.OnPlayerActionPerformed += HandlePlayerActionPerformed;
        gameManager.OnPhaseChanged += HandlePhaseChanged;
        gameManager.OnRoundChanged += HandleRoundChanged;
        gameManager.Dialogs.OnPhasePopupRequested += ShowPhasePopup;
        gameManager.Dialogs.OnPhasePopupDismissed += HidePhasePopup;
        gameManager.Dialogs.OnMessagePopupRequested += ShowMessagePopup;
        gameManager.Dialogs.OnPlayerSelectionChanged += SetPlayerPanelsSelectable;
        gameManager.OnCurrentPlayerChanged += HandleCurrentPlayerChanged;
        gameManager.OnAttackEffectExecuted += HandleAttackEffectExecuted;
        RefreshAll();
    }

    private void OnDisable()
    {
        if (gameManager == null)
            return;

        gameManager.Boss.OnHPChanged -= HandleHPChanged;
        gameManager.Boss.OnShieldChanged -= HandleShieldChanged;
        gameManager.Boss.OnBossAttackPlanned -= HandleBossAttackPlanned;
        gameManager.Boss.OnPlayerAttackDamageChanged -= HandlePlayerAttackDamageChanged;
        gameManager.OnPlayerActionPerformed -= HandlePlayerActionPerformed;
        gameManager.OnPhaseChanged -= HandlePhaseChanged;
        gameManager.OnRoundChanged -= HandleRoundChanged;
        gameManager.Dialogs.OnPhasePopupRequested -= ShowPhasePopup;
        gameManager.Dialogs.OnPhasePopupDismissed -= HidePhasePopup;
        gameManager.Dialogs.OnMessagePopupRequested -= ShowMessagePopup;
        gameManager.Dialogs.OnPlayerSelectionChanged -= SetPlayerPanelsSelectable;
        gameManager.OnCurrentPlayerChanged -= HandleCurrentPlayerChanged;
        gameManager.OnAttackEffectExecuted -= HandleAttackEffectExecuted;
    }

    private void BuildPlayerPanels()
    {
        foreach (PlayerPanelView panel in _playerPanels)
            panel.OnClicked -= HandlePlayerPanelClicked;
        _playerPanels.Clear();

        if (_playerRow == null || gameManager == null)
            return;

        _playerRow.Clear();
        foreach (Player player in gameManager.Players)
        {
            var panel = new PlayerPanelView(player);
            panel.OnClicked += HandlePlayerPanelClicked;
            player.OnActionsChanged += HandlePlayerActionsChanged;
            _playerPanels.Add(panel);
            _playerRow.Add(panel.Root);
        }
    }

    private void HandlePlayerPanelClicked(Player player)
    {
        if (gameManager == null || !gameManager.Dialogs.IsSelectingPlayer)
            return;

        gameManager.HandlePlayerPanelClicked(player);
    }

    private void SetPlayerPanelsSelectable(bool selectable)
    {
        foreach (PlayerPanelView panel in _playerPanels)
            panel.SetSelectable(selectable);
    }

    private void PlayTestCard()
    {
        if (gameManager == null || _cardIdInput == null)
            return;

        if (gameManager.UseCardByQrId(_cardIdInput.value))
            _cardIdInput.value = string.Empty;
    }

    private void ShowPhasePopup(GamePhase phase, string instructionText)
    {
        if (_phasePopup == null || _phasePopupTitle == null || _phasePopupMessage == null)
            return;

        _phasePopupTitle.text = phase.ToString();
        _phasePopupMessage.text = instructionText;
        _phasePopup.style.display = DisplayStyle.Flex;

        // Start-player selection is a non-blocking notification: hide the dim/close and
        // let clicks pass through to the player panels beneath.
        bool isStartSelection = gameManager != null && gameManager.Dialogs.IsStartPlayerSelectionActive;
        if (_closePhasePopupButton != null)
            _closePhasePopupButton.style.display = isStartSelection ? DisplayStyle.None : DisplayStyle.Flex;
        _phasePopup.EnableInClassList("phase-popup--notification", isStartSelection);
        SetPickingThrough(_phasePopup, isStartSelection);
    }

    // Recursively disables pointer picking so the notification never intercepts clicks.
    private void SetPickingThrough(VisualElement element, bool passThrough)
    {
        if (element == null)
            return;

        element.pickingMode = passThrough ? PickingMode.Ignore : PickingMode.Position;
        foreach (VisualElement child in element.Children())
            SetPickingThrough(child, passThrough);
    }

    private void ClosePhasePopup()
    {
        if (_phasePopup != null)
            _phasePopup.style.display = DisplayStyle.None;

        if (gameManager != null)
            gameManager.DismissPhasePopup();
    }

    private void HidePhasePopup()
    {
        if (_phasePopup != null)
            _phasePopup.style.display = DisplayStyle.None;
    }

    private void ShowMessagePopup(string title, string message)
    {
        if (_messagePopup == null || _messagePopupTitle == null || _messagePopupText == null)
            return;

        _messagePopupTitle.text = title;
        _messagePopupText.text = message;
        _messagePopup.style.display = DisplayStyle.Flex;
    }

    private void CloseMessagePopup()
    {
        if (_messagePopup != null)
            _messagePopup.style.display = DisplayStyle.None;

        if (gameManager != null)
            gameManager.DismissMessagePopup();
    }

    private void HandleCardIdKeyDown(KeyDownEvent eventData)
    {
        if (eventData.keyCode != KeyCode.Return && eventData.keyCode != KeyCode.KeypadEnter)
            return;

        PlayTestCard();
        eventData.StopPropagation();
    }

    private void HandlePlayerActionPerformed(Player player)
    {
        GetPanel(player)?.SetActionsRemaining(player.ActionsRemaining);
    }

    private void HandlePlayerActionsChanged(Player player)
    {
        GetPanel(player)?.SetActionsRemaining(player.ActionsRemaining);
    }

    private void HandleCurrentPlayerChanged(Player player)
    {
        UpdateActivePlayerPanel();
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.Action)
            RefreshAllPanels();
    }

    private void HandleRoundChanged(int round)
    {
        if (_roundLabel != null)
            _roundLabel.text = $"Round {round}";
    }

    private void HandleAttackEffectExecuted(DamageType type)
    {
        SetShieldHighlight(type, true);
    }

    private void HandleBossAttackPlanned(PlannedBossAttack attack)
    {
        RefreshAttackDisplays();
    }

    private void HandlePlayerAttackDamageChanged(Player player, int damage)
    {
        RefreshAttackDisplays();
    }

    private void RefreshAttackDisplays()
    {
        BossController boss = gameManager.Boss;
        foreach (PlayerPanelView panel in _playerPanels)
        {
            PlannedBossAttack attack = boss.GetPlannedAttack(panel.Player);
            string text = attack == null ? "0" : attack.Damage.ToString() +
                (attack.StatusEffect != StatusEffectType.None ? $" + {attack.StatusEffect}" : string.Empty);
            panel.SetAttackText(text);
        }
    }

    private void HandleHPChanged(int hp, int maxHp)
    {
        if (_hpLabel == null)
            return;

        _hpLabel.text = $"{hp} / {maxHp}";
        _hpFill.style.width = new Length(maxHp > 0 ? (float)hp / maxHp * 100f : 0f, LengthUnit.Percent);
    }

    private void HandleShieldChanged(DamageType type, int value)
    {
        switch (type)
        {
            case DamageType.Melee: if (_meleeShieldLabel != null) _meleeShieldLabel.text = value.ToString(); break;
            case DamageType.Ranged: if (_rangedShieldLabel != null) _rangedShieldLabel.text = value.ToString(); break;
            case DamageType.Magic: if (_magicShieldLabel != null) _magicShieldLabel.text = value.ToString(); break;
        }
    }

    private void RefreshAll()
    {
        if (gameManager == null || _bossNameLabel == null)
            return;

        BossController boss = gameManager.Boss;
        _bossNameLabel.text = boss.BossName;
        HandleHPChanged(boss.CurrentHP, boss.MaxHP);
        HandleShieldChanged(DamageType.Melee, boss.MeleeShield);
        HandleShieldChanged(DamageType.Ranged, boss.RangedShield);
        HandleShieldChanged(DamageType.Magic, boss.MagicShield);
        HandleRoundChanged(gameManager.Round);
        RefreshShieldHighlights();
        RefreshAllPanels();
        UpdateActivePlayerPanel();
        SetPlayerPanelsSelectable(gameManager.Dialogs.IsSelectingPlayer);
    }

    private void UpdateActivePlayerPanel()
    {
        foreach (PlayerPanelView panel in _playerPanels)
            panel.SetActive(panel.Player == gameManager.CurrentPlayer);
    }

    private void RefreshAllPanels()
    {
        RefreshAttackDisplays();
        foreach (PlayerPanelView panel in _playerPanels)
            panel.SetActionsRemaining(panel.Player.ActionsRemaining);
    }

    private PlayerPanelView GetPanel(Player player)
    {
        foreach (PlayerPanelView panel in _playerPanels)
        {
            if (panel.Player == player)
                return panel;
        }
        return null;
    }

    private void RefreshShieldHighlights()
    {
        SetShieldHighlight(DamageType.Melee, gameManager.WasAttackTypePlayedThisRound(DamageType.Melee));
        SetShieldHighlight(DamageType.Ranged, gameManager.WasAttackTypePlayedThisRound(DamageType.Ranged));
        SetShieldHighlight(DamageType.Magic, gameManager.WasAttackTypePlayedThisRound(DamageType.Magic));
    }

    private void SetShieldHighlight(DamageType type, bool isActive)
    {
        VisualElement icon = type switch
        {
            DamageType.Melee => _meleeShieldIcon,
            DamageType.Ranged => _rangedShieldIcon,
            DamageType.Magic => _magicShieldIcon,
            _ => null
        };

        icon?.EnableInClassList("shield-icon--active", isActive);
    }
}
