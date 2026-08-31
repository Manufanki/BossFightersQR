using System;
using UnityEngine;

// Fires at the end of a round in which a single hit dealt at least the threshold damage
// (hits only happen in the Action phase, so there is no phase setting).
[Serializable]
public class BossHitReactionTrigger : BossTrigger
{
    [Tooltip("Fires when a single hit deals at least this much damage during the round.")]
    public int hitThreshold = 5;
    public bool oneShot;
}
