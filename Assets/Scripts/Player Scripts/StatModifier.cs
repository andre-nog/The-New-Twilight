using System;

[Serializable]
public class StatModifier
{
    public StatType stat;
    public int amount;
}

// A ordem/posição de cada valor é o que fica serializado nos ItemSO existentes
// (Unity salva enum por índice, não por nome) — por isso Defense e Damage foram
// renomeados no lugar em vez de removidos, e os novos stats entram só no final.
public enum StatType
{
    Health,
    MaxHealth,

    Strength,
    Armor,          // era Defense
    Agility,
    Intelligence,

    AttackPower,    // era Damage — bônus plano, some ao Attack Power derivado da classe
    MoveSpeed,

    CriticalChance,
    CriticalDamage,

    // Novos — sempre adicionar ao final para não deslocar os índices acima
    SpellPower,
    MaxMana,
    HealthRegen,
    ManaRegen,
    Haste
}
