using System.Collections.Generic;
using UnityEngine;

// A boss definition: base stats, attacks, and the modular trigger list.
[CreateAssetMenu(fileName = "NewBoss", menuName = "BossFightersQR/Boss Data")]
public class BossData : ScriptableObject
{
    public string bossName;
    public int maxHP = 50;
    public BossShieldSet initialShields;

    public List<BossAttack> attacks = new List<BossAttack>();

    [Header("Modular Abilities")]
    [SerializeReference] public List<BossTrigger> modularTriggers = new List<BossTrigger>();
}
