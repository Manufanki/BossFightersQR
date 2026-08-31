using System;

// Boosts the next attack effect the same player resolves by a configurable amount.
[Serializable]
public class AttackBoostCardEffect : CardEffect
{
    public EffectValue boost;
}
