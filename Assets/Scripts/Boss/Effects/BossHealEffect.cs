using System;

// Restores boss HP instantly, capped at max HP.
[Serializable]
public class BossHealEffect : BossEffect
{
    public EffectValue amount;
}
