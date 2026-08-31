using System;

// Restores health; players apply it to their physical Health counter.
[Serializable]
public class HealCardEffect : CardEffect
{
    public EffectValue healing;
    public TargetMode targetMode = TargetMode.OnePlayer;
}
