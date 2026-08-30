using System;

// Grants the current player immediate additional actions.
[Serializable]
public class LightningCardEffect : CardEffect
{
    public int additionalActions = 1;
}
