using System;
using System.Collections.Generic;
using UnityEngine;

// Runtime state and behaviour for a boss fight, initialized from a BossData asset.
// Player HP is tracked manually outside the app; this class only tracks the boss itself
// and raises events describing damage players should manually apply to themselves.
public class BossController
{
    private BossData _data;
    private IReadOnlyList<Player> _players = new List<Player>();

    public BossData Data => _data;
    public string BossName { get; private set; }
    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }
    public int MeleeShield { get; private set; }
    public int RangedShield { get; private set; }
    public int MagicShield { get; private set; }
    public int AttackBonusDamage { get; private set; }
    public int PoisonTokens { get; private set; }
    public List<PlannedBossAttack> PlannedAttacks { get; } = new List<PlannedBossAttack>();
    public bool IsDefeated => CurrentHP <= 0;

    public event Action<int, int> OnHPChanged;
    public event Action<int> OnPoisonTokensChanged;
    public event Action<DamageType, int> OnShieldChanged;
    public event Action<DamageType> OnShieldDestroyed;
    public event Action<PlannedBossAttack> OnBossAttackPlanned;
    public event Action<PlannedBossAttack> OnBossAttackExecuted;
    public event Action<Player, int> OnPlayerAttackDamageChanged;
    // Fired for every hit taken, with the damage dealt (drives modular hit-threshold triggers).
    public event Action<int> OnHitTaken;

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
        PoisonTokens = 0;
        PlannedAttacks.Clear();

        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    public void SetPlayers(IReadOnlyList<Player> players)
    {
        _players = players ?? new List<Player>();
    }

    // Refills shields to the boss's configured starting values (called each Shield Phase).
    public void ResetShields()
    {
        MeleeShield = _data.initialShields.melee;
        RangedShield = _data.initialShields.ranged;
        MagicShield = _data.initialShields.magic;
        _disarmedShieldTriggers.Clear();
        _poisonOnShieldBreak.Clear();

        OnShieldChanged?.Invoke(DamageType.Melee, MeleeShield);
        OnShieldChanged?.Invoke(DamageType.Ranged, RangedShield);
        OnShieldChanged?.Invoke(DamageType.Magic, MagicShield);
    }

    // Plans one attack per player, each with its own rolled damage (called each Planning Phase).
    public void PlanAttacks()
    {
        PlannedAttacks.Clear();

        if (_data.attacks == null || _data.attacks.Count == 0 || _players.Count == 0)
            return;

        BossAttack chosen = _data.attacks[UnityEngine.Random.Range(0, _data.attacks.Count)];
        foreach (Player target in ResolveTargets(chosen))
        {
            var planned = new PlannedBossAttack(chosen, target, AttackBonusDamage);
            PlannedAttacks.Add(planned);
            OnPlayerAttackDamageChanged?.Invoke(target, planned.Damage);
        }
    }

    private List<Player> ResolveTargets(BossAttack attack)
    {
        var targets = new List<Player>();
        switch (attack.target)
        {
            case BossAttackTarget.SingleRandomPlayer:
                targets.Add(_players[UnityEngine.Random.Range(0, _players.Count)]);
                break;
            case BossAttackTarget.AllPlayers:
            case BossAttackTarget.EachPlayer:
            default:
                targets.AddRange(_players);
                break;
        }
        return targets;
    }

    // Protection effects reduce the boss attack against the given hero (floored at 0).
    public void ReducePlayerAttackDamage(Player target, int amount)
    {
        PlannedBossAttack attack = GetPlannedAttack(target);
        if (attack == null || amount <= 0)
            return;

        attack.ReduceDamage(amount);
        OnPlayerAttackDamageChanged?.Invoke(target, attack.Damage);
    }

    public PlannedBossAttack GetPlannedAttack(Player player)
    {
        foreach (PlannedBossAttack attack in PlannedAttacks)
        {
            if (attack.Target == player)
                return attack;
        }
        return null;
    }

    // Cleanse removes the status effect of the boss attack against the given hero.
    public void RemovePlayerAttackStatusEffect(Player target)
    {
        PlannedBossAttack attack = GetPlannedAttack(target);
        if (attack == null)
            return;

        attack.RemoveStatusEffect();
        OnPlayerAttackDamageChanged?.Invoke(target, attack.Damage);
    }

    // Fires OnBossAttackExecuted for each planned attack (called during Attack Phase).
    public void ExecutePlannedAttacks()
    {
        foreach (PlannedBossAttack attack in PlannedAttacks)
        {
            OnBossAttackExecuted?.Invoke(attack);
        }
    }

    public void AddPoisonTokens(int amount)
    {
        if (amount <= 0)
            return;

        PoisonTokens += amount;
        OnPoisonTokensChanged?.Invoke(PoisonTokens);
    }

    public void AddAttackBonus(int amount)
    {
        if (amount <= 0)
            return;

        AttackBonusDamage += amount;
    }

    public void IncreaseShield(DamageType type, int amount)
    {
        if (amount <= 0 || type == DamageType.True || type == DamageType.Poison || type == DamageType.None)
            return;

        int value = GetShield(type) + amount;
        SetShield(type, value);
        OnShieldChanged?.Invoke(type, value);
    }

    // Restores HP directly, capped at max HP.
    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    public Player GetRandomPlayer()
    {
        if (_players == null || _players.Count == 0)
            return null;

        return _players[UnityEngine.Random.Range(0, _players.Count)];
    }

    // Called each Status phase: every poison token deals 1 damage to the boss's HP.
    public void TickPoison()
    {
        if (PoisonTokens <= 0)
            return;

        CurrentHP = Mathf.Max(0, CurrentHP - PoisonTokens);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
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

    private readonly HashSet<DamageType> _disarmedShieldTriggers = new HashSet<DamageType>();
    private readonly Dictionary<DamageType, int> _poisonOnShieldBreak = new Dictionary<DamageType, int>();

    // Damages only the given shield (no HP overflow). Fires its destroy trigger unless disarmed.
    public void DamageShield(DamageType type, int amount)
    {
        if (amount <= 0)
            return;

        int shieldBefore = GetShield(type);
        int shieldAfter = Mathf.Max(0, shieldBefore - amount);
        SetShield(type, shieldAfter);
        OnShieldChanged?.Invoke(type, shieldAfter);

        if (shieldBefore > 0 && shieldAfter == 0)
            HandleShieldDestroyed(type);
    }

    // Arms the shield: breaking it adds the given poison tokens once (consumed on break).
    public void ArmShieldBreakPoison(DamageType type, int amount)
    {
        if (amount > 0)
            _poisonOnShieldBreak[type] = amount;
    }

    // The next destruction of this shield fires no shield trigger (consumed on use).
    public void DisarmShieldTrigger(DamageType type)
    {
        _disarmedShieldTriggers.Add(type);
    }

    private void HandleShieldDestroyed(DamageType type)
    {
        // Disarmed shields (Shield Strike suppress) do not raise the destroy event.
        if (_disarmedShieldTriggers.Remove(type))
            return;

        OnShieldDestroyed?.Invoke(type);

        if (_poisonOnShieldBreak.TryGetValue(type, out int poison))
        {
            _poisonOnShieldBreak.Remove(type);
            AddPoisonTokens(poison);
        }
    }

    // Applies damage of the given type: shield absorbs first, any overflow hits HP.
    public void TakeDamage(int amount, DamageType type)
    {
        if (amount <= 0)
            return;

        // True damage bypasses shields entirely and hits HP directly.
        if (type == DamageType.True)
        {
            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);
            OnHitTaken?.Invoke(amount);
            return;
        }

        // Poison does not touch shields or HP now; it adds tokens that tick each Status phase.
        if (type == DamageType.Poison)
        {
            AddPoisonTokens(amount);
            OnHitTaken?.Invoke(amount);
            return;
        }

        int shieldBefore = GetShield(type);
        int shieldDamage = Mathf.Min(amount, shieldBefore);
        int overflow = amount - shieldDamage;
        int shieldAfter = shieldBefore - shieldDamage;
        SetShield(type, shieldAfter);
        OnShieldChanged?.Invoke(type, shieldAfter);

        if (shieldBefore > 0 && shieldAfter == 0)
            HandleShieldDestroyed(type);

        if (overflow > 0)
        {
            CurrentHP = Mathf.Max(0, CurrentHP - overflow);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);
        }

        OnHitTaken?.Invoke(amount);
    }
}
