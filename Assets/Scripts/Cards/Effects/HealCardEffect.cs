using System;

// Restores health; players apply it to their physical Health counter.
[Serializable]
public class HealCardEffect : CardEffect
{
    public int healing;
    public TargetMode targetMode = TargetMode.OnePlayer;
}
