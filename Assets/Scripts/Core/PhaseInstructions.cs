using System;
using UnityEngine;

// Inspector-editable instruction text shown in each phase's popup.
[Serializable]
public class PhaseInstructions
{
    [TextArea] public string planning = "The boss chooses its attack for this round.";
    [TextArea] public string shield = "Restore the boss shields for this round.";
    [TextArea] public string action = "Players may scan and play their action cards.";
    [TextArea] public string attack = "Resolve the boss's planned attack.";
    [TextArea] public string status = "Resolve active status effects.";
    [TextArea] public string dropCards = "Discard any cards you do not want to keep.";
    [TextArea] public string drawCards = "Draw cards for the next round.";
}
