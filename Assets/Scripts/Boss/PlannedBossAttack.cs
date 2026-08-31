// Runtime state for one planned boss attack. Damage/effect are copies of the BossData
// attack so Protection can reduce them without mutating the shared ScriptableObject.
public class PlannedBossAttack
{
    private readonly BossAttack _source;

    public string Name => _source.name;
    public string Description => _source.description;
    public Player Target { get; }
    public int Damage { get; private set; }
    public StatusEffectType StatusEffect { get; private set; }

    public PlannedBossAttack(BossAttack source, Player target, int bonusDamage = 0)
    {
        _source = source;
        Target = target;
        Damage = UnityEngine.Random.Range(source.minDamage, source.maxDamage + 1) + bonusDamage;
        StatusEffect = source.statusEffect;
    }

    // Protection effects reduce damage (floored at 0); at 0 the additional effect is lost too.
    public void ReduceDamage(int amount)
    {
        if (amount <= 0)
            return;

        Damage = System.Math.Max(0, Damage - amount);
        if (Damage == 0)
            StatusEffect = StatusEffectType.None;
    }

    // Cleanse removes only the additional effect; the damage stays the same.
    public void RemoveStatusEffect()
    {
        StatusEffect = StatusEffectType.None;
    }
}
