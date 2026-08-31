using System;

// Instructs the player to draw cards from their physical draw pile.
[Serializable]
public class DrawCardEffect : CardEffect
{
    public EffectValue cardsToDraw;
    public TargetMode targetMode = TargetMode.OnePlayer;
}
