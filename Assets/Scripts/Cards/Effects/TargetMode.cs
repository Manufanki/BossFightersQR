// How a card effect picks which players it applies to.
// PreviousTarget reuses the target chosen for the prior effect on the same card.
public enum TargetMode
{
    ActivePlayer,
    OnePlayer,
    TwoPlayers,
    AllPlayers,
    PreviousTarget
}
