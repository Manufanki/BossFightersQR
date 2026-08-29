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

public sealed class PlanningPhase : PopupPhase
{
    public override GamePhase PhaseId => GamePhase.Planning;
    public PlanningPhase(string instructionText) : base(instructionText) { }
}

public sealed class ShieldPhase : PopupPhase
{
    public override GamePhase PhaseId => GamePhase.Shield;
    public ShieldPhase(string instructionText) : base(instructionText) { }
}

public sealed class AttackPhase : PopupPhase
{
    public override GamePhase PhaseId => GamePhase.Attack;
    public AttackPhase(string instructionText) : base(instructionText) { }
}

public sealed class StatusPhase : PopupPhase
{
    public override GamePhase PhaseId => GamePhase.Status;
    public StatusPhase(string instructionText) : base(instructionText) { }
}

public sealed class DropCardsPhase : PopupPhase
{
    public override GamePhase PhaseId => GamePhase.DropCards;
    public DropCardsPhase(string instructionText) : base(instructionText) { }
}

public sealed class DrawCardsPhase : PopupPhase
{
    public override GamePhase PhaseId => GamePhase.DrawCards;
    public DrawCardsPhase(string instructionText) : base(instructionText) { }
}