public enum GamePhase
{
    Planning,
    Shield,
    Action,
    Attack,
    Status,
    DropCards,
    DrawCards
}

public enum DamageType
{
    Melee,
    Ranged,
    Magic,
}

public enum StatusEffectType
{
    None,
    Poison
}

// Placeholder: extend with the actual hero types from the board game.
public enum HeroType
{
    All,
    Dwarf,
    Elf,
    Troll,
    Halfing,
}

// Placeholder: extend with the actual class types from the board game.
public enum ClassType
{
    All,
    Warrior,
    Mage,
    Rogue,
    Druid,
}
