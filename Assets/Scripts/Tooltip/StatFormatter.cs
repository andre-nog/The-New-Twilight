public static class StatFormatter
{
    public static string GetStatName(StatType stat)
    {
        switch (stat)
        {
            case StatType.MaxHealth:
                return "Max Health";

            case StatType.MoveSpeed:
                return "Move Speed";

            case StatType.CriticalChance:
                return "Critical Chance";

            case StatType.CriticalDamage:
                return "Critical Damage";

            case StatType.AttackPower:
                return "Attack Power";

            case StatType.SpellPower:
                return "Spell Power";

            case StatType.MaxMana:
                return "Max Mana";

            case StatType.HealthRegen:
                return "Health Regen";

            case StatType.ManaRegen:
                return "Mana Regen";

            default:
                return stat.ToString();
        }
    }
}
