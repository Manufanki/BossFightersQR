using System;
using UnityEngine;

// Retaliation triggered when a single hit deals at least the threshold damage.
[Serializable]
public class BossReaction
{
    [Tooltip("Retaliation triggers when a single hit deals at least this much damage.")]
    public int damageThreshold;
    public int retaliationDamage;
    [TextArea] public string description;
}
