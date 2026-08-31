using System;

// Increases the boss's bonus attack damage, added to every attack it plans.
[Serializable]
public class BossAttackUpEffect : BossEffect
{
    public EffectValue amount;
}
