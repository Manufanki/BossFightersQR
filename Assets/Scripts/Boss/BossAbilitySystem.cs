using System;
using System.Collections.Generic;

// Executes the modular boss triggers configured on BossData: each trigger subclass has
// its own condition (round parity, HP threshold, shield-break event, heavy-hit reaction)
// and one event is raised per due trigger; GameManager shows the popup and applies effects.
public class BossAbilitySystem
{
    private readonly BossController _boss;
    private readonly IReadOnlyList<Player> _players;
    private readonly HashSet<int> _firedOneShots = new HashSet<int>();
    private readonly HashSet<string> _firedThisPhase = new HashSet<string>();
    private readonly HashSet<int> _firedShieldBreaks = new HashSet<int>();
    private readonly HashSet<int> _hitThresholdMetThisRound = new HashSet<int>();

    public event Action<BossTrigger> OnTriggerDue;
    // Set when a shield-break trigger fired while it could not be shown yet; the owner
    // (GameManager) calls FlushQueuedTriggers at a safe point to re-emit them.
    public bool HasDeferredTriggers { get; private set; }

    public BossAbilitySystem(BossController boss, IReadOnlyList<Player> players)
    {
        _boss = boss;
        _players = players;

        _boss.OnShieldDestroyed += HandleShieldDestroyed;
        _boss.OnHitTaken += HandleHitTaken;
    }

    // Shield-break triggers fire immediately when their shield is destroyed (unless the
    // destruction was suppressed, in which case the event never fires). Once per refill.
    private void HandleShieldDestroyed(DamageType type)
    {
        if (_boss.Data?.modularTriggers == null)
            return;

        for (int i = 0; i < _boss.Data.modularTriggers.Count; i++)
        {
            if (!(_boss.Data.modularTriggers[i] is BossShieldBrokenTrigger trigger) || trigger.shieldType != type)
                continue;

            if (_firedShieldBreaks.Contains(i))
                continue;

            _firedShieldBreaks.Add(i);
            // Deferred: fired synchronously inside card resolution; surfaced between effects.
            _queuedShieldBreakTriggers.Add(trigger);
            HasDeferredTriggers = true;
        }
    }

    private readonly List<BossTrigger> _queuedShieldBreakTriggers = new List<BossTrigger>();

    // Re-emits shield-break triggers queued while a card was resolving. Call only when
    // no popup is open and no card is mid-resolution.
    public void FlushQueuedTriggers()
    {
        HasDeferredTriggers = false;

        foreach (BossTrigger trigger in _queuedShieldBreakTriggers)
            OnTriggerDue?.Invoke(trigger);

        _queuedShieldBreakTriggers.Clear();
    }

    private void HandleHitTaken(int damage)
    {
        if (_boss.Data?.modularTriggers == null)
            return;

        for (int i = 0; i < _boss.Data.modularTriggers.Count; i++)
        {
            if (_boss.Data.modularTriggers[i] is BossHitReactionTrigger trigger && damage >= trigger.hitThreshold)
                _hitThresholdMetThisRound.Add(i);
        }
    }

    // Clears round-scoped condition state (call on Action phase entry).
    public void ResetRound()
    {
        _hitThresholdMetThisRound.Clear();
    }

    public void EvaluatePhaseEntry(GamePhase phase, int round)
    {
        _firedThisPhase.Clear();

        if (_boss.Data == null || _boss.Data.modularTriggers == null)
            return;

        for (int i = 0; i < _boss.Data.modularTriggers.Count; i++)
        {
            BossTrigger trigger = _boss.Data.modularTriggers[i];
            if (!IsDue(trigger, i, phase, round))
                continue;

            _firedThisPhase.Add(Key(phase, i));
            if (IsOneShot(trigger))
                _firedOneShots.Add(i);

            OnTriggerDue?.Invoke(trigger);
        }
    }

    private bool IsDue(BossTrigger trigger, int index, GamePhase phase, int round)
    {
        if (trigger == null)
            return false;

        // Shield-break triggers fire on the destroy event itself, never at phase entry.
        if (trigger is BossShieldBrokenTrigger)
            return false;

        if (IsOneShot(trigger) && _firedOneShots.Contains(index))
            return false;

        // Re-entry in the same phase (phase popups re-enter) must not refire a trigger.
        if (_firedThisPhase.Contains(Key(phase, index)))
            return false;

        switch (trigger)
        {
            case BossRoundTrigger roundTrigger:
                if (roundTrigger.phase != phase || round < roundTrigger.fromRound)
                    return false;

                switch (roundTrigger.timing)
                {
                    case BossTriggerTiming.EvenRounds:
                        return round % 2 == 0;
                    case BossTriggerTiming.OddRounds:
                        return round % 2 == 1;
                    case BossTriggerTiming.SpecificRound:
                        return round == roundTrigger.specificRound;
                    default:
                        return true;
                }

            case BossHealthTrigger healthTrigger:
                return healthTrigger.phase == phase && _boss.CurrentHP <= healthTrigger.hpAtOrBelow;

            case BossHitReactionTrigger _:
                // Hits only happen in the Action phase; the reaction fires at round end.
                return phase == GamePhase.DrawCards && _hitThresholdMetThisRound.Contains(index);

            default:
                return false;
        }
    }

    private static bool IsOneShot(BossTrigger trigger)
    {
        switch (trigger)
        {
            // HP thresholds and shield breaks are inherently one-time events.
            case BossHealthTrigger _:
            case BossShieldBrokenTrigger _:
                return true;
            case BossRoundTrigger roundTrigger:
                return roundTrigger.oneShot;
            case BossHitReactionTrigger hitTrigger:
                return hitTrigger.oneShot;
            default:
                return false;
        }
    }

    // Executes all effects of a fired trigger against the boss and the players.
    public void ApplyEffects(BossTrigger trigger, int round, Action<string> log)
    {
        if (trigger?.effects == null)
            return;

        foreach (BossEffect effect in trigger.effects)
            ApplyEffect(effect, round, log);
    }

    private void ApplyEffect(BossEffect effect, int round, Action<string> log)
    {
        switch (effect)
        {
            case BossAttackUpEffect attackUp:
                int bonus = attackUp.amount.Evaluate(round);
                _boss.AddAttackBonus(bonus);
                log?.Invoke($"Boss attack damage increased by {bonus}.");
                break;

            case BossDamagePlayersEffect damage:
                int damageAmount = damage.damage.Evaluate(round);
                // Hero health is tracked on physical counters; the popup instructs the players.
                string damageTargets = damage.hitAllPlayers ? "All heroes" : $"Player {_boss.GetRandomPlayer()?.PlayerNumber}";
                log?.Invoke($"{damageTargets} take {damageAmount} damage on their counter(s).");
                break;

            case BossStatusPlayersEffect status:
                int tokens = status.tokens.Evaluate(round);
                string targets = status.hitAllPlayers ? "All heroes" : $"Player {_boss.GetRandomPlayer()?.PlayerNumber}";
                // Status points are tracked on physical counters; the app only instructs.
                log?.Invoke($"{targets} gain {tokens} {status.statusEffect} token(s) on their counter(s).");
                break;

            case BossShieldUpEffect shieldUp:
                int shieldAmount = shieldUp.amount.Evaluate(round);
                _boss.IncreaseShield(shieldUp.shieldType, shieldAmount);
                log?.Invoke($"Boss {shieldUp.shieldType} shield increased by {shieldAmount} to {_boss.GetShield(shieldUp.shieldType)}.");
                break;

            case BossPoisonEffect poison:
                int poisonTokens = poison.tokens.Evaluate(round);
                _boss.AddPoisonTokens(poisonTokens);
                log?.Invoke($"Boss gains {poisonTokens} poison token(s).");
                break;

            case BossHealEffect heal:
                int healAmount = heal.amount.Evaluate(round);
                _boss.Heal(healAmount);
                log?.Invoke($"Boss recovers {healAmount} HP.");
                break;

            case null:
                log?.Invoke("Boss trigger contains an empty effect entry.");
                break;

            default:
                log?.Invoke($"Unhandled boss effect: {effect.GetType().Name}");
                break;
        }
    }

    // Popup shown for a fired trigger: configured text, or a summary of its effects.
    public static string DescribeTrigger(BossTrigger trigger)
    {
        if (!string.IsNullOrWhiteSpace(trigger.popupText))
            return trigger.popupText;

        var parts = new List<string>();
        if (trigger.effects != null)
        {
            foreach (BossEffect effect in trigger.effects)
            {
                switch (effect)
                {
                    case BossAttackUpEffect e: parts.Add($"+{e.amount} attack damage"); break;
                    case BossDamagePlayersEffect e: parts.Add($"{e.damage} damage to {(e.hitAllPlayers ? "all heroes" : "one hero")}"); break;
                    case BossStatusPlayersEffect e: parts.Add($"{e.tokens} {e.statusEffect} token(s) to {(e.hitAllPlayers ? "all heroes" : "one hero")}"); break;
                    case BossShieldUpEffect e: parts.Add($"+{e.amount} {e.shieldType} shield"); break;
                    case BossPoisonEffect e: parts.Add($"+{e.tokens} poison token(s)"); break;
                    case BossHealEffect e: parts.Add($"boss heals {e.amount} HP"); break;
                }
            }
        }
        return parts.Count > 0 ? string.Join(", ", parts) : "Nothing happens.";
    }

    private static string Key(GamePhase phase, int index) => $"{phase}:{index}";
}
