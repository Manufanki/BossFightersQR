using System;

// Owns the state of the two UI dialogs (phase instructions and generic messages) and
// raises events when the UI should show them. State survives UI reloads so a popup is
// never lost when PanelRenderer rebuilds its visual tree.
public class GameDialogs
{
    public bool HasActivePhasePopup { get; private set; }
    public GamePhase ActivePopupPhase { get; private set; }
    public string ActivePopupText { get; private set; }
    public bool HasActiveMessagePopup { get; private set; }
    public string ActiveMessageTitle { get; private set; }
    public string ActiveMessageText { get; private set; }
    public bool IsSelectingPlayer { get; private set; }
    public bool IsStartPlayerSelectionActive { get; set; }

    public event Action<GamePhase, string> OnPhasePopupRequested;
    public event Action OnPhasePopupDismissed;
    public event Action<string, string> OnMessagePopupRequested;
    public event Action<bool> OnPlayerSelectionChanged;

    public void RequestPhasePopup(GamePhase phase, string instructionText)
    {
        HasActivePhasePopup = true;
        ActivePopupPhase = phase;
        ActivePopupText = instructionText;
        OnPhasePopupRequested?.Invoke(phase, instructionText);
    }

    public void DismissPhasePopup()
    {
        HasActivePhasePopup = false;
        OnPhasePopupDismissed?.Invoke();
    }

    public void RequestMessage(string title, string message)
    {
        HasActiveMessagePopup = true;
        ActiveMessageTitle = title;
        ActiveMessageText = message;
        OnMessagePopupRequested?.Invoke(title, message);
    }

    public void DismissMessage()
    {
        HasActiveMessagePopup = false;
    }

    public void BeginPlayerSelection()
    {
        IsSelectingPlayer = true;
        OnPlayerSelectionChanged?.Invoke(true);
    }

    public void EndPlayerSelection()
    {
        IsSelectingPlayer = false;
        OnPlayerSelectionChanged?.Invoke(false);
    }
}
