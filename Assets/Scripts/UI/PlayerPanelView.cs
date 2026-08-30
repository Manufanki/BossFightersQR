using System;
using UnityEngine;
using UnityEngine.UIElements;

// Runtime-built UI panel for one player: boss attack value, action dots, and a
// clickable state used to pick the target of a Protection effect.
public class PlayerPanelView
{
    private readonly Label _attackValue;
    private readonly VisualElement[] _actionDots;

    public Player Player { get; }
    public VisualElement Root { get; }
    public event Action<Player> OnClicked;

    public PlayerPanelView(Player player)
    {
        Player = player;

        Root = new VisualElement();
        Root.AddToClassList("player-panel");
        Root.RegisterCallback<ClickEvent>(_ => OnClicked?.Invoke(player));

        var header = new Label($"P{player.PlayerNumber}  {player.HeroType} {player.ClassType}");
        header.AddToClassList("player-panel-header");
        Root.Add(header);

        var attackDisplay = new VisualElement();
        attackDisplay.AddToClassList("attack-display");
        attackDisplay.Add(new Label("Boss Attack") { name = "attack-caption" });
        _attackValue = new Label("0");
        _attackValue.AddToClassList("attack-value");
        attackDisplay.Add(_attackValue);
        Root.Add(attackDisplay);

        var actionRow = new VisualElement();
        actionRow.AddToClassList("action-row");
        _actionDots = new VisualElement[3];
        for (int i = 0; i < _actionDots.Length; i++)
        {
            var dot = new VisualElement();
            dot.AddToClassList("action-indicator");
            _actionDots[i] = dot;
            actionRow.Add(dot);
        }
        Root.Add(actionRow);
    }

    public void SetAttackText(string text)
    {
        _attackValue.text = text;
    }

    public void SetActionsRemaining(int actionsRemaining)
    {
        for (int i = 0; i < _actionDots.Length; i++)
            _actionDots[i].style.display = i < actionsRemaining ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetSelectable(bool selectable)
    {
        Root.EnableInClassList("player-panel--selectable", selectable);
    }

    public void SetActive(bool isActive)
    {
        Root.EnableInClassList("player-panel--active", isActive);
    }
}
