using System;

// Starting shield values per damage type, refilled each Shield phase.
[Serializable]
public struct BossShieldSet
{
    public int melee;
    public int ranged;
    public int magic;
}
