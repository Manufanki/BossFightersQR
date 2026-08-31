using System;

// Players remove their status tokens physically; the app shows the instruction.
[Serializable]
public class RemoveStatusCardEffect : CardEffect
{
    public EffectValue tokensToRemove;
    public TargetMode targetMode = TargetMode.OnePlayer;
}
