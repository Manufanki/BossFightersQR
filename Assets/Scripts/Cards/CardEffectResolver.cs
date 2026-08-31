using System;
using System.Collections.Generic;

// Executes the effect queue of the player whose card is being resolved, one effect at a
// time. Every effect pauses that player's queue until acknowledged (popup close or target
// pick). Interruption turns nest cleanly because each player owns their own queue.
public class CardEffectResolver
{
    private readonly BossController _boss;
    private readonly Action<string> _log;
    private readonly Action<string, string> _showMessage;
    private readonly Action<ProtectionCardEffect> _requestProtectionTarget;
    private readonly Action<ExtraTurnCardEffect, Player> _requestExtraTurnTarget;
    private readonly Action<CleanseAttackCardEffect> _requestCleanseTarget;
    private readonly Action<ShieldStrikeCardEffect> _requestShieldTarget;
    private readonly Func<int> _getCurrentRound;
    private readonly HashSet<DamageType> _attackTypesPlayedThisRound = new HashSet<DamageType>();

    public event Action<DamageType, int> OnAttackEffectExecuted;
    public event Action<Player> OnCardResolved;

    public bool WasAttackTypePlayedThisRound(DamageType type) => _attackTypesPlayedThisRound.Contains(type);

    public CardEffectResolver(BossController boss, Action<string> log, Action<string, string> showMessage,
        Action<ProtectionCardEffect> requestProtectionTarget, Action<ExtraTurnCardEffect, Player> requestExtraTurnTarget,
        Action<CleanseAttackCardEffect> requestCleanseTarget, Func<int> getCurrentRound,
        Action<ShieldStrikeCardEffect> requestShieldTarget)
    {
        _boss = boss;
        _log = log;
        _showMessage = showMessage;
        _requestProtectionTarget = requestProtectionTarget;
        _requestExtraTurnTarget = requestExtraTurnTarget;
        _requestCleanseTarget = requestCleanseTarget;
        _getCurrentRound = getCurrentRound;
        _requestShieldTarget = requestShieldTarget;
    }

    // Supports only work if a matching Attack was played earlier in the same round.
    public void ResetRound()
    {
        _attackTypesPlayedThisRound.Clear();
    }

    public void Resolve(Player player, CardData card)
    {
        player.BeginCard(card);

        if (!player.HasQueuedEffects)
        {
            Report(player, $"'{card.cardName}' has no effects configured.");
            return;
        }

        ProcessQueue(player);
    }

    // Call when the player's pending input (popup close or target pick) is handled.
    public void CompleteInteraction(Player player)
    {
        if (player == null || !player.IsWaitingForInteraction)
            return;

        player.ResumeFromInteraction();
        ProcessQueue(player);
    }

    private void ProcessQueue(Player player)
    {
        while (player.HasQueuedEffects && !player.IsWaitingForInteraction)
            Dispatch(player, player.DequeueEffect());

        if (!player.HasQueuedEffects && !player.IsWaitingForInteraction && player.CardInPlay != null)
        {
            player.CompleteCard();
            OnCardResolved?.Invoke(player);
        }
    }

    private void Dispatch(Player player, CardEffect effect)
    {
        int total = player.CardInPlay?.effects?.Count ?? 0;
        _log($"Player {player.PlayerNumber} resolving effect {total - player.QueuedEffectCount}/{total}: {effect?.GetType().Name}");

        switch (effect)
        {
            case AttackCardEffect attack:
                ResolveAttackEffect(player, attack);
                break;
            case AttackBoostCardEffect attackBoost:
                ResolveAttackBoostEffect(player, attackBoost);
                break;
            case ShieldStrikeCardEffect shieldStrike:
                ResolveShieldStrikeEffect(player, shieldStrike);
                break;
            case SupportCardEffect support:
                ResolveSupportEffect(player, support);
                break;
            case ProtectionCardEffect protection:
                ResolveProtectionEffect(player, protection);
                break;
            case LightningCardEffect lightning:
                ResolveLightningEffect(player, lightning);
                break;
            case HealCardEffect heal:
                ResolveHealEffect(player, heal);
                break;
            case DrawCardEffect draw:
                ResolveDrawEffect(player, draw);
                break;
            case RemoveStatusCardEffect removeStatus:
                ResolveRemoveStatusEffect(player, removeStatus);
                break;
            case ExtraTurnCardEffect extraTurn:
                ResolveExtraTurnEffect(player, extraTurn);
                break;
            case CleanseAttackCardEffect cleanse:
                ResolveCleanseAttackEffect(player, cleanse);
                break;
            case SpecialCardEffect special:
                ResolveSpecialEffect(player, special);
                break;
            case null:
                Report(player, $"'{player.CardInPlay?.cardName}' contains an empty effect entry.");
                break;
        }
    }

    private void ResolveAttackEffect(Player player, AttackCardEffect effect)
    {
        int amount = effect.damage.Evaluate(_getCurrentRound(), BossAttackAgainst(player));
        int boost = player.ConsumePendingAttackBoost();
        amount += boost;
        _boss.TakeDamage(amount, effect.damageType);
        _attackTypesPlayedThisRound.Add(effect.damageType);
        OnAttackEffectExecuted?.Invoke(effect.damageType, amount);
        Report(player, boost > 0
            ? $"Player {player.PlayerNumber} used a boosted {effect.damageType} attack for {amount} damage (+{boost} boost)."
            : $"Player {player.PlayerNumber} used a {effect.damageType} attack for {amount} damage.");
    }

    private void ResolveAttackBoostEffect(Player player, AttackBoostCardEffect effect)
    {
        int amount = effect.boost.Evaluate(_getCurrentRound(), BossAttackAgainst(player));
        player.AddPendingAttackBoost(amount);
        Report(player, $"Player {player.PlayerNumber}'s next attack is boosted by {amount}.");
    }

    private void ResolveShieldStrikeEffect(Player player, ShieldStrikeCardEffect effect)
    {
        int amount = effect.damage.Evaluate(_getCurrentRound(), BossAttackAgainst(player));
        Report(player, $"After closing this, click a boss shield to reduce it by {amount}.");
        _requestShieldTarget(effect);
    }

    private void ResolveSupportEffect(Player player, SupportCardEffect effect)
    {
        if (!_attackTypesPlayedThisRound.Contains(effect.damageType))
        {
            Report(player, $"A {effect.damageType} attack must be played earlier this round.");
            return;
        }

        int amount = effect.damage.Evaluate(_getCurrentRound(), BossAttackAgainst(player));
        _boss.TakeDamage(amount, effect.damageType);
        Report(player, $"Player {player.PlayerNumber} used a {effect.damageType} support for {amount} damage.");
    }

    private void ResolveProtectionEffect(Player player, ProtectionCardEffect effect)
    {
        int amount = effect.protection.Evaluate(_getCurrentRound(), BossAttackAgainst(player));

        if (TryReusePreviousTarget(player, effect.targetMode, out Player previous))
        {
            _boss.ReducePlayerAttackDamage(previous, amount);
            Report(player, $"Protection: Player {previous.PlayerNumber}'s boss attack is reduced by {amount}.");
            return;
        }

        Report(player, $"Protection: after closing this, choose a player to reduce their boss attack by {amount}.");
        _requestProtectionTarget(effect);
    }

    private void ResolveLightningEffect(Player player, LightningCardEffect effect)
    {
        player.AddActions(effect.additionalActions);
        player.MarkCardGrantedExtraAction(effect.additionalActions);
        Report(player, $"Player {player.PlayerNumber} gains {effect.additionalActions} additional action(s).");
    }

    private void ResolveHealEffect(Player player, HealCardEffect effect)
    {
        // Hero health is intentionally tracked on physical counters, not in the app.
        int amount = effect.healing.Evaluate(_getCurrentRound(), BossAttackAgainst(player));
        Report(player, $"{DescribeTargets(player, effect.targetMode)} recover {amount} health on their counter(s).");
    }

    private void ResolveDrawEffect(Player player, DrawCardEffect effect)
    {
        // Physical draw piles are handled by the players; the app only reports the instruction.
        int amount = effect.cardsToDraw.Evaluate(_getCurrentRound(), BossAttackAgainst(player));
        Report(player, $"{DescribeTargets(player, effect.targetMode)} draw {amount} card(s).");
    }

    private void ResolveRemoveStatusEffect(Player player, RemoveStatusCardEffect effect)
    {
        int amount = effect.tokensToRemove.Evaluate(_getCurrentRound(), BossAttackAgainst(player));
        Report(player, $"{DescribeTargets(player, effect.targetMode)} may remove {amount} status token(s).");
    }

    private void ResolveExtraTurnEffect(Player player, ExtraTurnCardEffect effect)
    {
        // ActivePlayer mode behaves exactly like Lightning: the acting player keeps the turn.
        if (effect.targetMode == TargetMode.ActivePlayer)
        {
            player.AddActions(1);
            player.MarkCardGrantedExtraAction(1);
            Report(player, $"Player {player.PlayerNumber} takes an additional action immediately.");
            return;
        }

        if (TryReusePreviousTarget(player, effect.targetMode, out Player previous))
        {
            Report(player, $"Player {previous.PlayerNumber} takes an immediate turn.");
            _requestExtraTurnTarget(effect, previous);
            return;
        }

        Report(player, "Choose a player to take an immediate turn (they may choose themselves).");
        _requestExtraTurnTarget(effect, null);
    }

    private void ResolveCleanseAttackEffect(Player player, CleanseAttackCardEffect effect)
    {
        if (TryReusePreviousTarget(player, effect.targetMode, out Player previous))
        {
            _boss.RemovePlayerAttackStatusEffect(previous);
            Report(player, $"Player {previous.PlayerNumber}'s boss attack loses its status effect.");
            return;
        }

        Report(player, "Choose a player whose boss attack loses its status effect (damage is unchanged).");
        _requestCleanseTarget(effect);
    }

    // PreviousTarget reuses the target from the prior effect on this card; falls back to
    // OnePlayer (prompt) when no target was chosen yet.
    private bool TryReusePreviousTarget(Player player, TargetMode mode, out Player target)
    {
        target = mode == TargetMode.PreviousTarget ? player.LastSelectedTarget : null;
        return target != null;
    }

    private void ResolveSpecialEffect(Player player, SpecialCardEffect effect)
    {
        Report(player, string.IsNullOrWhiteSpace(effect.description) ? "Special effect resolved." : effect.description);
    }

    private string DescribeTargets(Player player, TargetMode mode)
    {
        switch (mode)
        {
            case TargetMode.AllPlayers: return "All players";
            case TargetMode.TwoPlayers: return "Two chosen players";
            case TargetMode.OnePlayer: return "One chosen player";
            default: return $"Player {player.PlayerNumber}";
        }
    }

    // The planned boss attack damage against the given player (0 if none is planned).
    private int BossAttackAgainst(Player player)
    {
        PlannedBossAttack attack = _boss.GetPlannedAttack(player);
        return attack?.Damage ?? 0;
    }

    // Reports an effect and pauses the player's queue until the popup is acknowledged.
    private void Report(Player player, string message)
    {
        _log(message);
        _showMessage("Card Effect", message);
        player.PauseForInteraction();
    }
}
