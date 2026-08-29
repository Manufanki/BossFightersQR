using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BossShieldSet
{
    public int melee;
    public int ranged;
    public int magic;
}

[Serializable]
public class BossAttack
{
    public string name;
    public int damage;
    public StatusEffectType statusEffect;
    [TextArea] public string description;
}

[Serializable]
public class BossReaction
{
    [Tooltip("Retaliation triggers when a single hit deals at least this much damage.")]
    public int damageThreshold;
    public int retaliationDamage;
    [TextArea] public string description;
}

[Serializable]
public class BossHPTrigger
{
    [Tooltip("Triggers once when boss HP drops to or below this value.")]
    public int hpThreshold;
    public int attackBonusDamage;
    [TextArea] public string description;
}

[Serializable]
public class BossTimeTrigger
{
    [Tooltip("Triggers at the end of this round number.")]
    public int triggerOnRound;
    public int damageToAllPlayers;
    [TextArea] public string description;
}

[Serializable]
public class BossShieldTrigger
{
    public DamageType shieldType;
    public int damageOnDestroy;
    [TextArea] public string description;
}

[CreateAssetMenu(fileName = "NewBoss", menuName = "BossFightersQR/Boss Data")]
public class BossData : ScriptableObject
{
    public string bossName;
    public int maxHP = 50;
    public BossShieldSet initialShields;

    public List<BossAttack> attacks = new List<BossAttack>();
    public List<BossReaction> reactions = new List<BossReaction>();
    public List<BossHPTrigger> hpTriggers = new List<BossHPTrigger>();
    public List<BossTimeTrigger> timeTriggers = new List<BossTimeTrigger>();
    public List<BossShieldTrigger> shieldTriggers = new List<BossShieldTrigger>();
}
