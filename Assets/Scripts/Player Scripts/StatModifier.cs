using System;

[Serializable]
public class StatModifier
{
    public StatType stat;
    public int amount;
}

public enum StatType
{
    Health,
    MaxHealth,

    Strength,
    Defense,
    Agility,
    Intelligence,

    Damage,
    MoveSpeed,

    CriticalChance,
    CriticalDamage
}