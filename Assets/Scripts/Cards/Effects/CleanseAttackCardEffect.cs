using System;

// Removes the status effect of the boss attack against the chosen player; damage is unchanged.
[Serializable]
public class CleanseAttackCardEffect : CardEffect
{
    public TargetMode targetMode = TargetMode.OnePlayer;
}
