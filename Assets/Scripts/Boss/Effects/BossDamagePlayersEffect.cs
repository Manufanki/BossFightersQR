using System;

// Deals instant damage to hero health: all players or one random player.
[Serializable]
public class BossDamagePlayersEffect : BossEffect
{
    public EffectValue damage;
    public bool hitAllPlayers = true;
}
