using System;

[Serializable]
public abstract class CardEffect
{
}

[Serializable]
public class AttackCardEffect : CardEffect
{
    public DamageType damageType;
    public int damage;
}

[Serializable]
public class SupportCardEffect : CardEffect
{
    public DamageType damageType;
    public int damage;
}

[Serializable]
public class ProtectionCardEffect : CardEffect
{
    public int protection;
}

[Serializable]
public class LightningCardEffect : CardEffect
{
    public int additionalActions = 1;
}

[Serializable]
public class HealCardEffect : CardEffect
{
    public int healing;
}

[Serializable]
public class DrawCardEffect : CardEffect
{
    public int cardsToDraw;
}

[Serializable]
public class SpecialCardEffect : CardEffect
{
    [UnityEngine.TextArea] public string description;
}