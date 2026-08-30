// Phase 2: the boss shields refill to their configured values.
public sealed class ShieldPhase : PopupPhase
{
    public override GamePhase PhaseId => GamePhase.Shield;
    public ShieldPhase(string instructionText) : base(instructionText) { }
}
