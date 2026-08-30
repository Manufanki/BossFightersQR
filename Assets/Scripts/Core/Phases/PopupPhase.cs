// Shared behaviour for phases that wait for the player to acknowledge their instruction popup.
public abstract class PopupPhase : IGamePhase
{
    private readonly string _instructionText;

    public abstract GamePhase PhaseId { get; }
    public bool IsComplete { get; private set; }

    protected PopupPhase(string instructionText)
    {
        _instructionText = instructionText;
    }

    public virtual void Enter(GameManager gameManager)
    {
        IsComplete = false;
        gameManager.Log($"Entering phase: {PhaseId}");
        gameManager.ShowPhasePopup(PhaseId, _instructionText);
    }

    public virtual void Tick(GameManager gameManager)
    {
    }

    public virtual void Exit(GameManager gameManager)
    {
    }

    public void Complete()
    {
        IsComplete = true;
    }
}
