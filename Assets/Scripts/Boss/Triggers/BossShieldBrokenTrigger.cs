using System;

// Fires the moment the chosen shield is destroyed. A shield destroyed with a suppressed
// trigger (Shield Strike) raises no event, so this trigger does not fire.
[Serializable]
public class BossShieldBrokenTrigger : BossTrigger
{
    public DamageType shieldType = DamageType.Melee;
}
