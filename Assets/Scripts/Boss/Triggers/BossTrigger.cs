using System;
using System.Collections.Generic;
using UnityEngine;

// Base class for modular boss triggers. Each subclass fires on its own condition and
// shows only its relevant fields in the inspector. The effect list runs when it fires.
[Serializable]
public abstract class BossTrigger
{
    public string triggerName = "New Trigger";

    [TextArea] public string popupText;

    [SerializeReference] public List<BossEffect> effects = new List<BossEffect>();
}
