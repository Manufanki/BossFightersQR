using System;
using UnityEngine;

// Which rounds a round-based trigger is eligible in.
public enum BossTriggerTiming { EveryRound, EvenRounds, OddRounds, SpecificRound }

// Fires at the chosen phase entry in matching rounds (parity or a specific round number).
[Serializable]
public class BossRoundTrigger : BossTrigger
{
    public BossTriggerTiming timing = BossTriggerTiming.EveryRound;
    [Tooltip("Used only with SpecificRound timing.")]
    public int specificRound = 1;
    [Tooltip("Round at which this trigger starts being eligible; 1 = from the start.")]
    public int fromRound = 1;
    public GamePhase phase = GamePhase.Status;
    public bool oneShot;
}
