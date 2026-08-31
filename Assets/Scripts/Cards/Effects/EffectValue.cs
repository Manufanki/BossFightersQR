using System;

// An effect amount that is either a fixed constant, a dynamic source such as the current
// round or the boss attack against the playing player, or an inclusive random range.
// Extensible: add a Mode value and one line in Evaluate.
[Serializable]
public struct EffectValue
{
    public enum Mode { Constant, CurrentRound, RandomRange, BossAttackAgainstPlayer }

    public Mode mode;
    public int constant;
    public int min;
    public int max;

    // bossAttackAgainstPlayer is the planned boss attack damage against the playing player.
    public int Evaluate(int currentRound, int bossAttackAgainstPlayer)
    {
        switch (mode)
        {
            case Mode.CurrentRound:
                return currentRound;
            case Mode.RandomRange:
                return UnityEngine.Random.Range(min, max + 1);
            case Mode.BossAttackAgainstPlayer:
                return bossAttackAgainstPlayer;
            default:
                return constant;
        }
    }

    public int Evaluate(int currentRound) => Evaluate(currentRound, 0);
}
