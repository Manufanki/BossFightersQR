using System;
using UnityEngine;
using UnityEngine.UIElements;

// Very basic HUD: boss name, an HP bar, and 3 shield icons with their current values.
// Uses PanelRenderer (the successor to UIDocument) and updates via BossController events.
[RequireComponent(typeof(PanelRenderer))]
public class GameHUD : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    private PanelRenderer _panelRenderer;
    private int _uiVersion = -1;
    private Label _bossNameLabel;
    private VisualElement _hpFill;
    private Label _hpLabel;
    private Label _meleeShieldLabel;
    private Label _rangedShieldLabel;
    private Label _magicShieldLabel;
    private Label _player1AttackLabel;
    private TextField _cardIdInput;
    private Button _playCardButton;
    private Button _nextPhaseButton;
    private VisualElement _phasePopup;
    private Label _phasePopupTitle;
    private Label _phasePopupMessage;
    private Button _closePhasePopupButton;
    private VisualElement _messagePopup;
    private Label _messagePopupTitle;
    private Label _messagePopupText;
    private Button _closeMessagePopupButton;
    private VisualElement[] _actionIndicators;

    private void Awake()
    {
        _panelRenderer = GetComponent<PanelRenderer>();

        // Ensures the HUD renders even if no PanelSettings asset was assigned in the Inspector.
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

    // PanelRenderer (re)builds its root VisualElement asynchronously, so element lookups happen here
    // rather than in Awake. The version check avoids re-querying on duplicate reload notifications.
    private void OnUIReload(PanelRenderer panelRenderer, VisualElement root, int version)
    {
        if (_uiVersion == version)
            return;
        _uiVersion = version;

        _bossNameLabel = root.Q<Label>("boss-name");
        _hpFill = root.Q<VisualElement>("hp-fill");
        _hpLabel = root.Q<Label>("hp-label");
        _meleeShieldLabel = root.Q<Label>("melee-shield-label");
        _rangedShieldLabel = root.Q<Label>("ranged-shield-label");
        _magicShieldLabel = root.Q<Label>("magic-shield-label");
        _player1AttackLabel = root.Q<Label>("player1-attack-label");
        _cardIdInput = root.Q<TextField>("card-id-input");
        _playCardButton = root.Q<Button>("play-card-button");
        _nextPhaseButton = root.Q<Button>("next-phase-button");
        _phasePopup = root.Q<VisualElement>("phase-popup");
        _phasePopupTitle = root.Q<Label>("phase-popup-title");
        _phasePopupMessage = root.Q<Label>("phase-popup-message");
        _closePhasePopupButton = root.Q<Button>("close-phase-popup-button");
        _messagePopup = root.Q<VisualElement>("message-popup");
        _messagePopupTitle = root.Q<Label>("message-popup-title");
        _messagePopupText = root.Q<Label>("message-popup-text");
        _closeMessagePopupButton = root.Q<Button>("close-message-popup-button");
        _actionIndicators = new[]
        {
            root.Q<VisualElement>("action-indicator-1"),
            root.Q<VisualElement>("action-indicator-2"),
            root.Q<VisualElement>("action-indicator-3")
        };

        if (_playCardButton != null)
            _playCardButton.clicked += PlayTestCard;
        if (_nextPhaseButton != null)
            _nextPhaseButton.clicked += AdvanceManualPhase;
        if (_closePhasePopupButton != null)
            _closePhasePopupButton.clicked += ClosePhasePopup;
        if (_closeMessagePopupButton != null)
            _closeMessagePopupButton.clicked += CloseMessagePopup;
        if (_cardIdInput != null)
            _cardIdInput.RegisterCallback<KeyDownEvent>(HandleCardIdKeyDown);

        if (gameManager != null && gameManager.HasActivePhasePopup)
            ShowPhasePopup(gameManager.ActivePopupPhase, gameManager.ActivePopupText);
        if (gameManager != null && gameManager.HasActiveMessagePopup)
            ShowMessagePopup(gameManager.ActiveMessageTitle, gameManager.ActiveMessageText);

        if (_bossNameLabel == null || _hpFill == null || _hpLabel == null
            || _meleeShieldLabel == null || _rangedShieldLabel == null || _magicShieldLabel == null
            || _player1AttackLabel == null)
        {
            Debug.LogError("[GameHUD] Could not find one or more BossHUD.uxml elements. " +
                "Check that the PanelRenderer's Visual Tree Asset is set to BossHUD.");
        }

        RefreshAll();
    }

    private void OnEnable()
    {
        if (gameManager == null)
            return;

        gameManager.Boss.OnHPChanged += HandleHPChanged;
        gameManager.Boss.OnShieldChanged += HandleShieldChanged;
        gameManager.Boss.OnBossAttackPlanned += HandleBossAttackPlanned;
        gameManager.OnPlayerActionPerformed += HandlePlayerActionPerformed;
        gameManager.OnPhaseChanged += HandlePhaseChanged;
        gameManager.OnPhasePopupRequested += ShowPhasePopup;
        gameManager.OnMessagePopupRequested += ShowMessagePopup;
        RefreshAll();
    }



    private void OnDisable()
    {
        if (gameManager == null)
            return;

        gameManager.Boss.OnHPChanged -= HandleHPChanged;
        gameManager.Boss.OnShieldChanged -= HandleShieldChanged;
        gameManager.Boss.OnBossAttackPlanned -= HandleBossAttackPlanned;
        gameManager.OnPlayerActionPerformed -= HandlePlayerActionPerformed;
        gameManager.OnPhaseChanged -= HandlePhaseChanged;
        gameManager.OnPhasePopupRequested -= ShowPhasePopup;
        gameManager.OnMessagePopupRequested -= ShowMessagePopup;
    }

    private void PlayTestCard()
    {
        if (gameManager == null || _cardIdInput == null)
            return;

        if (gameManager.UseCardByQrId(_cardIdInput.value))
            _cardIdInput.value = string.Empty;
    }

    private void AdvanceManualPhase()
    {
        if (gameManager != null)
            gameManager.CompleteManualPhase();
    }

    private void ShowPhasePopup(GamePhase phase, string instructionText)
    {
        if (_phasePopup == null || _phasePopupTitle == null || _phasePopupMessage == null)
            return;

        _phasePopupTitle.text = phase.ToString();
        _phasePopupMessage.text = instructionText;
        _phasePopup.style.display = DisplayStyle.Flex;
    }

    private void ClosePhasePopup()
    {
        if (_phasePopup != null)
            _phasePopup.style.display = DisplayStyle.None;

        if (gameManager != null)
            gameManager.DismissPhasePopup();
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
        RefreshActionIndicators(player.ActionsRemaining);
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.Action)
            RefreshActionIndicators(gameManager.CurrentActionPlayer.ActionsRemaining);
    }
    private void HandleBossAttackPlanned(BossAttack attack)
    {
        Debug.Log($"[GameHUD] Boss planned attack: {attack.name} (Damage: {attack.damage}, StatusEffect: {attack.statusEffect})");
        if (_player1AttackLabel == null)
            return;

        _player1AttackLabel.text = attack.damage.ToString() +
            (attack.statusEffect != StatusEffectType.None ? $" + {attack.statusEffect}" : string.Empty);
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

        if (boss.PlannedAttacks.Count > 0)
            HandleBossAttackPlanned(boss.PlannedAttacks[0]);

        RefreshActionIndicators(gameManager.CurrentActionPlayer?.ActionsRemaining ?? 0);
    }

    private void RefreshActionIndicators(int actionsRemaining)
    {
        if (_actionIndicators == null)
            return;

        for (int i = 0; i < _actionIndicators.Length; i++)
        {
            if (_actionIndicators[i] != null)
                _actionIndicators[i].style.display = i < actionsRemaining ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
