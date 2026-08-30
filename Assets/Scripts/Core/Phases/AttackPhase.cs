// Phase 4: the boss executes the attacks planned in Phase 1.
public sealed class AttackPhase : PopupPhase
{
    public override GamePhase PhaseId => GamePhase.Attack;
    public AttackPhase(string instructionText) : base(instructionText) { }
}
