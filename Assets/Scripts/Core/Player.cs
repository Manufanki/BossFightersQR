using System;
using System.Collections.Generic;
using UnityEngine;

// A hero seat at the table: identity, the per-round action budget, and the turn state
// for card resolution (cards played this round + the pending effect queue of the card
// currently in play). HP, hands, and piles stay physical per the rulebook.
[Serializable]
public class Player
{
    [SerializeField] private int playerNumber;
    [SerializeField] private HeroType heroType;
    [SerializeField] private ClassType classType;

    private readonly List<CardData> _playedCards = new List<CardData>();
    private readonly Queue<CardEffect> _effectQueue = new Queue<CardEffect>();

    public int PlayerNumber => playerNumber;
    public HeroType HeroType => heroType;
    public ClassType ClassType => classType;

    public int ActionsRemaining { get; private set; }
    public bool HasActionsRemaining => ActionsRemaining > 0;

    public CardData CardInPlay { get; private set; }
    public IReadOnlyList<CardData> PlayedCards => _playedCards;
    public bool HasQueuedEffects => _effectQueue.Count > 0;
    public int QueuedEffectCount => _effectQueue.Count;
    public bool IsWaitingForInteraction { get; private set; }
    public bool CardGrantedExtraAction { get; private set; }

    public event Action<Player> OnActionsChanged;

    public Player(int playerNumber)
    {
        this.playerNumber = playerNumber;
    }

    // Resets the action budget and clears all per-round turn state (called each Action phase).
    public void StartRound(int actionsPerRound)
    {
        ActionsRemaining = actionsPerRound;
        _playedCards.Clear();
        _effectQueue.Clear();
        CardInPlay = null;
        IsWaitingForInteraction = false;
        CardGrantedExtraAction = false;
        OnActionsChanged?.Invoke(this);
    }

    public void UseAction()
    {
        if (ActionsRemaining > 0)
        {
            ActionsRemaining--;
            OnActionsChanged?.Invoke(this);
        }
    }

    public void AddActions(int amount)
    {
        if (amount > 0)
        {
            ActionsRemaining += amount;
            OnActionsChanged?.Invoke(this);
        }
    }

    // Stores the card and fills this player's effect queue for resolution.
    public void BeginCard(CardData card)
    {
        CardInPlay = card;
        CardGrantedExtraAction = false;
        _playedCards.Add(card);
        _effectQueue.Clear();

        if (card.effects != null)
        {
            foreach (CardEffect effect in card.effects)
                _effectQueue.Enqueue(effect);
        }
    }

    public CardEffect DequeueEffect()
    {
        return _effectQueue.Count > 0 ? _effectQueue.Dequeue() : null;
    }

    public void MarkCardGrantedExtraAction()
    {
        CardGrantedExtraAction = true;
    }

    public void PauseForInteraction()
    {
        IsWaitingForInteraction = true;
    }

    public void ResumeFromInteraction()
    {
        IsWaitingForInteraction = false;
    }

    public void CompleteCard()
    {
        CardInPlay = null;
        _effectQueue.Clear();
    }
}
