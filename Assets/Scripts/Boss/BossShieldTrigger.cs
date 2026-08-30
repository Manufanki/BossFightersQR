using System;
using UnityEngine;

// Ability that fires when the boss shield of the given type is destroyed.
[Serializable]
public class BossShieldTrigger
{
    public DamageType shieldType;
    public int damageOnDestroy;
    [TextArea] public string description;
}
