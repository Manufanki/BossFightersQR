using System;

// Players remove their status tokens physically; the app shows the instruction.
[Serializable]
public class RemoveStatusCardEffect : CardEffect
{
    public int tokensToRemove = 1;
    public TargetMode targetMode = TargetMode.OnePlayer;
}
