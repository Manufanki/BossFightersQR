using System;

// Adds poison tokens to the boss (they tick HP each Status phase).
[Serializable]
public class BossPoisonEffect : BossEffect
{
    public EffectValue tokens;
}
