// Phase 6: players discard played cards and respect their hand limit.
public sealed class DropCardsPhase : PopupPhase
{
    public override GamePhase PhaseId => GamePhase.DropCards;
    public DropCardsPhase(string instructionText) : base(instructionText) { }
}
