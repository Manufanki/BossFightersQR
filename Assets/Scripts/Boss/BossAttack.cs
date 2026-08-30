using System;
using UnityEngine;

// How a boss attack picks its targets each round.
public enum BossAttackTarget
{
    AllPlayers,
    SingleRandomPlayer,
    EachPlayer
}

// One boss attack the boss can plan in Phase 1 and execute in Phase 4.
[Serializable]
public class BossAttack
{
    public string name;
    public BossAttackTarget target = BossAttackTarget.AllPlayers;
    public int minDamage;
    public int maxDamage;
    public StatusEffectType statusEffect;
    [TextArea] public string description;
}
