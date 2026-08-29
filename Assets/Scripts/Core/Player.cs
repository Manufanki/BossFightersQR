using UnityEngine;

[System.Serializable]
// Tracks only the per-round action budget; player stats (HP, hand, etc.) are tracked externally.
public class Player
{
    [SerializeField] private int playerNumber;
    [SerializeField] private HeroType heroType;
    [SerializeField] private ClassType classType;

    public int PlayerNumber => playerNumber;
    public HeroType HeroType => heroType;
    public ClassType ClassType => classType;
    public int ActionsRemaining { get; private set; }
    public bool HasActionsRemaining => ActionsRemaining > 0;

    public Player(int playerNumber)
    {
        this.playerNumber = playerNumber;
    }

    public void ResetActions(int actionsPerRound)
    {
        ActionsRemaining = actionsPerRound;
    }

    public void UseAction()
    {
        if (ActionsRemaining > 0)
            ActionsRemaining--;
    }

    public void AddActions(int amount)
    {
        if (amount > 0)
            ActionsRemaining += amount;
    }
}
