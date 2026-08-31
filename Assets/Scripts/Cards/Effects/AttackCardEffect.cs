using System;

// Deals typed damage to the boss and enables Supports of the same type this round.
[Serializable]
public class AttackCardEffect : CardEffect
{
    public DamageType damageType;
    public EffectValue damage;
}
