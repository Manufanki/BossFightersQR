using System;
using UnityEngine;

// Recurring ability that fires at the end of a specific round.
[Serializable]
public class BossTimeTrigger
{
    [Tooltip("Triggers at the end of this round number.")]
    public int triggerOnRound;
    public int damageToAllPlayers;
    [TextArea] public string description;
}
