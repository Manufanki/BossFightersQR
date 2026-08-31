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

    private List<CardData> _playedCards = new List<CardData>();
    private Queue<CardEffect> _effectQueue = new Queue<CardEffect>();

    public int PlayerNumber => playerNumber;
    public HeroType HeroType => heroType;
    public ClassType ClassType => classType;

    public int ActionsRemaining { get; private set; }
    public bool HasActionsRemaining => ActionsRemaining > 0;

    public CardData CardInPlay { get; private set; }
    public Player LastSelectedTarget { get; private set; }
    public IReadOnlyList<CardData> PlayedCards => PlayedCardsList;
    public bool HasQueuedEffects => EffectQueue.Count > 0;
    public int QueuedEffectCount => EffectQueue.Count;
    public bool IsWaitingForInteraction { get; private set; }
    public int ExtraActionsGrantedByCard { get; private set; }

    // Bonus damage added to this player's next attack effect, then consumed.
    public int PendingAttackBoost { get; private set; }

    // Unity-deserialized instances skip the constructor, so initialize these on first use.
    private List<CardData> PlayedCardsList => _playedCards ??= new List<CardData>();
    private Queue<CardEffect> EffectQueue => _effectQueue ??= new Queue<CardEffect>();

    public event Action<Player> OnActionsChanged;

    public Player(int playerNumber)
    {
        this.playerNumber = playerNumber;
    }

    // Resets the action budget and clears all per-round turn state (called each Action phase).
    public void StartRound(int actionsPerRound)
    {
        ActionsRemaining = actionsPerRound;
        PlayedCardsList.Clear();
        EffectQueue.Clear();
        CardInPlay = null;
        IsWaitingForInteraction = false;
        ExtraActionsGrantedByCard = 0;
        PendingAttackBoost = 0;
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
    // Extra-action grants are not reset here: they persist across the kept turns they buy.
    public void BeginCard(CardData card)
    {
        CardInPlay = card;
        LastSelectedTarget = null;
        PlayedCardsList.Add(card);
        EffectQueue.Clear();

        if (card.effects != null)
        {
            foreach (CardEffect effect in card.effects)
                EffectQueue.Enqueue(effect);
        }
    }

    public CardEffect DequeueEffect()
    {
        return EffectQueue.Count > 0 ? EffectQueue.Dequeue() : null;
    }

    public void MarkCardGrantedExtraAction(int count)
    {
        ExtraActionsGrantedByCard += count;
    }

    // Returns true and decrements while the current card still has unused extra actions.
    public bool TryConsumeExtraAction()
    {
        if (ExtraActionsGrantedByCard <= 0)
            return false;

        ExtraActionsGrantedByCard--;
        return true;
    }

    public void PauseForInteraction()
    {
        IsWaitingForInteraction = true;
    }

    public void SelectTarget(Player target)
    {
        LastSelectedTarget = target;
    }

    public void ResumeFromInteraction()
    {
        IsWaitingForInteraction = false;
    }

    public void AddPendingAttackBoost(int amount)
    {
        if (amount > 0)
            PendingAttackBoost += amount;
    }

    // Returns the pending boost once and clears it.
    public int ConsumePendingAttackBoost()
    {
        int boost = PendingAttackBoost;
        PendingAttackBoost = 0;
        return boost;
    }

    public void CompleteCard()
    {
        CardInPlay = null;
        EffectQueue.Clear();
    }
}
