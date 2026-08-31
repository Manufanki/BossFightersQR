using System;

// Gives status effect points to players (tracked on their physical counters).
[Serializable]
public class BossStatusPlayersEffect : BossEffect
{
    public StatusEffectType statusEffect;
    public EffectValue tokens;
    public bool hitAllPlayers = true;
}
