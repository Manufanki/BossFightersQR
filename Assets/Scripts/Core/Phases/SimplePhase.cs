// Minimal IGamePhase used until each phase gets its real (Boss/Card-driven) logic.
// autoComplete phases (Planning, Shield, Attack, Status) finish the instant they're entered;
// manual phases (Action, DropCards, DrawCards) wait for Complete() to be called, e.g. by a UI button.
public class SimplePhase : IGamePhase
{
    private readonly bool _autoComplete;

    public GamePhase PhaseId { get; }
    public bool IsComplete { get; private set; }

    public SimplePhase(GamePhase phaseId, bool autoComplete)
    {
        PhaseId = phaseId;
        _autoComplete = autoComplete;
    }

    public void Enter(GameManager gameManager)
    {
        IsComplete = _autoComplete;
        gameManager.Log($"Entering phase: {PhaseId}");
    }

    public void Tick(GameManager gameManager)
    {
    }

    public void Exit(GameManager gameManager)
    {
    }

    public void Complete()
    {
        IsComplete = true;
    }
}
