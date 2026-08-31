using System;

// Damages one boss shield chosen by clicking its icon in the UI. Optionally suppresses
// the boss reaction trigger that would normally fire if this strike destroys the shield.
[Serializable]
public class ShieldStrikeCardEffect : CardEffect
{
    public EffectValue damage;

    // When true, destroying the shield with this strike does not fire its shield trigger.
    public bool suppressShieldTrigger;

    // When > 0, the shield is armed: breaking it (with this strike or any later hit)
    // adds this many poison tokens to the boss, once.
    public EffectValue poisonOnBreak;
}
