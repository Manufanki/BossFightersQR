// Phase 7: players draw back up to their hand limit.
public sealed class DrawCardsPhase : PopupPhase
{
    public override GamePhase PhaseId => GamePhase.DrawCards;
    public DrawCardsPhase(string instructionText) : base(instructionText) { }
}
