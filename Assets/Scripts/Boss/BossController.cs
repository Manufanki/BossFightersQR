using System;
using System.Collections.Generic;
using UnityEngine;

// Runtime state and behaviour for a boss fight, initialized from a BossData asset.
// Player HP is tracked manually outside the app; this class only tracks the boss itself
// and raises events describing damage players should manually apply to themselves.
public class BossController
{
    private BossData _data;
    private readonly HashSet<int> _firedHpThresholds = new HashSet<int>();
    private readonly HashSet<int> _firedTimeRounds = new HashSet<int>();

    public string BossName { get; private set; }
    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }
    public int MeleeShield { get; private set; }
    public int RangedShield { get; private set; }
    public int MagicShield { get; private set; }
    public int AttackBonusDamage { get; private set; }
    public List<BossAttack> PlannedAttacks { get; } = new List<BossAttack>();
    public bool IsDefeated => CurrentHP <= 0;

    public event Action<int, int> OnHPChanged;
    public event Action<DamageType, int> OnShieldChanged;
    public event Action<DamageType> OnShieldDestroyed;
    public event Action<BossAttack> OnBossAttackPlanned;
    public event Action<BossAttack> OnBossAttackExecuted;
    public event Action<BossReaction> OnReactionTriggered;
    public event Action<BossHPTrigger> OnHPTriggerFired;
    public event Action<BossTimeTrigger> OnTimeTriggerFired;
    public event Action<BossShieldTrigger> OnShieldTriggerFired;

    public void Initialize(BossData data)
    {
        _data = data;
        BossName = data.bossName;
        MaxHP = data.maxHP;
        CurrentHP = data.maxHP;
        MeleeShield = data.initialShields.melee;
        RangedShield = data.initialShields.ranged;
        MagicShield = data.initialShields.magic;
        AttackBonusDamage = 0;
        PlannedAttacks.Clear();
        _firedHpThresholds.Clear();
        _firedTimeRounds.Clear();

        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    // Refills shields to the boss's configured starting values (called each Shield Phase).
    public void ResetShields()
    {
        MeleeShield = _data.initialShields.melee;
        RangedShield = _data.initialShields.ranged;
        MagicShield = _data.initialShields.magic;

        OnShieldChanged?.Invoke(DamageType.Melee, MeleeShield);
        OnShieldChanged?.Invoke(DamageType.Ranged, RangedShield);
        OnShieldChanged?.Invoke(DamageType.Magic, MagicShield);
    }

    // Picks one random attack for the round from BossData.attacks (called each Planning Phase).
    public void PlanAttacks()
    {
        PlannedAttacks.Clear();

        if (_data.attacks == null || _data.attacks.Count == 0)
            return;

        BossAttack chosen = _data.attacks[UnityEngine.Random.Range(0, _data.attacks.Count)];
        PlannedAttacks.Add(chosen);
        OnBossAttackPlanned?.Invoke(chosen); // Notify that the boss has planned this attack (for UI display).
    }

    // Fires OnBossAttack for each planned attack (called during Attack Phase).
    public void ExecutePlannedAttacks()
    {
        foreach (BossAttack attack in PlannedAttacks)
        {
            OnBossAttackExecuted?.Invoke(attack);
        }
    }

    public int GetShield(DamageType type)
    {
        switch (type)
        {
            case DamageType.Melee: return MeleeShield;
            case DamageType.Ranged: return RangedShield;
            case DamageType.Magic: return MagicShield;
            default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private void SetShield(DamageType type, int value)
    {
        switch (type)
        {
            case DamageType.Melee: MeleeShield = value; break;
            case DamageType.Ranged: RangedShield = value; break;
            case DamageType.Magic: MagicShield = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    // Applies damage of the given type: shield absorbs first, any overflow hits HP.
    public void TakeDamage(int amount, DamageType type)
    {
        if (amount <= 0)
            return;

        int shieldBefore = GetShield(type);
        int shieldDamage = Mathf.Min(amount, shieldBefore);
        int overflow = amount - shieldDamage;
        int shieldAfter = shieldBefore - shieldDamage;
        SetShield(type, shieldAfter);
        OnShieldChanged?.Invoke(type, shieldAfter);

        if (shieldBefore > 0 && shieldAfter == 0)
        {
            OnShieldDestroyed?.Invoke(type);
            EvaluateShieldTriggers(type);
        }

        if (overflow > 0)
        {
            CurrentHP = Mathf.Max(0, CurrentHP - overflow);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);
            EvaluateHPTriggers();
        }

        EvaluateReactions(amount);
    }

    // Sums retaliation damage from reactions whose threshold this single hit met or exceeded.
    private void EvaluateReactions(int damageDealt)
    {
        if (_data.reactions == null)
            return;

        foreach (BossReaction reaction in _data.reactions)
        {
            if (damageDealt >= reaction.damageThreshold)
                OnReactionTriggered?.Invoke(reaction);
        }
    }

    // Fires each HP trigger at most once, the first time CurrentHP drops to or below its threshold.
    private void EvaluateHPTriggers()
    {
        if (_data.hpTriggers == null)
            return;

        for (int i = 0; i < _data.hpTriggers.Count; i++)
        {
            BossHPTrigger trigger = _data.hpTriggers[i];
            if (_firedHpThresholds.Contains(i))
                continue;

            if (CurrentHP <= trigger.hpThreshold)
            {
                _firedHpThresholds.Add(i);
                AttackBonusDamage += trigger.attackBonusDamage;
                OnHPTriggerFired?.Invoke(trigger);
            }
        }
    }

    // Fires each time trigger at most once, the round it's configured for. Call at round-end.
    public void EvaluateTimeTriggers(int currentRound)
    {
        if (_data.timeTriggers == null)
            return;

        foreach (BossTimeTrigger trigger in _data.timeTriggers)
        {
            int key = trigger.triggerOnRound;
            if (trigger.triggerOnRound != currentRound || _firedTimeRounds.Contains(key))
                continue;

            _firedTimeRounds.Add(key);
            OnTimeTriggerFired?.Invoke(trigger);
        }
    }

    private void EvaluateShieldTriggers(DamageType destroyedType)
    {
        if (_data.shieldTriggers == null)
            return;

        foreach (BossShieldTrigger trigger in _data.shieldTriggers)
        {
            if (trigger.shieldType == destroyedType)
                OnShieldTriggerFired?.Invoke(trigger);
        }
    }
}
