using System;
using UnityEngine;

// One-time attack bonus gained when boss HP drops to or below a threshold.
[Serializable]
public class BossHPTrigger
{
    [Tooltip("Triggers once when boss HP drops to or below this value.")]
    public int hpThreshold;
    public int attackBonusDamage;
    [TextArea] public string description;
}
