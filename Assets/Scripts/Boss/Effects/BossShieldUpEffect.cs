using System;

// Raises one boss shield by a configurable amount.
[Serializable]
public class BossShieldUpEffect : BossEffect
{
    public DamageType shieldType = DamageType.Melee;
    public EffectValue amount;
}
