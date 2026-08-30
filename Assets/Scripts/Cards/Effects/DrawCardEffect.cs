using System;

// Instructs the player to draw cards from their physical draw pile.
[Serializable]
public class DrawCardEffect : CardEffect
{
    public int cardsToDraw;
    public TargetMode targetMode = TargetMode.OnePlayer;
}
