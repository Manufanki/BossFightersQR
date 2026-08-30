using System;

// Deals typed damage, but only if a matching Attack was already played this round.
[Serializable]
public class SupportCardEffect : CardEffect
{
    public DamageType damageType;
    public int damage;
}
