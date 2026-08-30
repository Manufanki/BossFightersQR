using System;

// Reduces the boss attack against one chosen hero in the Attack phase.
[Serializable]
public class ProtectionCardEffect : CardEffect
{
    public int protection;
    public TargetMode targetMode = TargetMode.OnePlayer;
}
