// Phase 5: status effects such as poison take effect.
public sealed class StatusPhase : PopupPhase
{
    public override GamePhase PhaseId => GamePhase.Status;
    public StatusPhase(string instructionText) : base(instructionText) { }
}
