using System.Collections.Generic;
using UnityEngine;

// A boss definition: base stats plus all five boss ability types from the rulebook.
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
