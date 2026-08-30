// Phase 1: the boss picks its attacks for the round.
public sealed class PlanningPhase : PopupPhase
{
    public override GamePhase PhaseId => GamePhase.Planning;
    public PlanningPhase(string instructionText) : base(instructionText) { }
}
