// Damage categories; Melee/Ranged/Magic map to boss shields, True bypasses shields to hit
// HP, and Poison adds tokens that tick the boss's HP each Status phase.
// None is a neutral value used for optional conditions (never dealt as damage).
public enum DamageType
{
    Melee,
    Ranged,
    Magic,
    True,
    Poison,
    None
}
