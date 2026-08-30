using System;

// The acting player chooses a player (including themselves) to take an immediate turn.
// With TargetMode.ActivePlayer it behaves like Lightning: the acting player gets the action.
[Serializable]
public class ExtraTurnCardEffect : CardEffect
{
    public TargetMode targetMode = TargetMode.OnePlayer;
}
