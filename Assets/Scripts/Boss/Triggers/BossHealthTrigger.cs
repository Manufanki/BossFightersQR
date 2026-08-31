using System;
using UnityEngine;

// Fires once at the chosen phase entry, as soon as boss HP is at or below the threshold.
[Serializable]
public class BossHealthTrigger : BossTrigger
{
    [Tooltip("Triggers when boss HP is at or below this value.")]
    public int hpAtOrBelow = 25;
    public GamePhase phase = GamePhase.Status;
}
