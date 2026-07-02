using UnityEngine;
using System;

// NOVO: garante que o Awake deste script rode antes de qualquer outro script
// com ordem padrão (como o StatsUI), evitando que StatsManager.Instance
// ainda esteja null quando outros scripts tentam se inscrever no evento.
[DefaultExecutionOrder(-100)]
public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;
    public event Action OnStatsChanged;

    [Header("Primary Attributes")]
    public int strength;
    public int defense;
    public int agility;
    public int intelligence;

    [Header("Combat Stats")]
    public int damage;
    public float attackRange;
    public float attackcooldown;

    [Header("Movement Stats")]
    public int moveSpeed;

    [Header("Health Stats")]
    public int maxHealth;
    public int currentHealth;

    [Header("Critical Stats")]
    [Range(0, 100)]
    public float criticalChance = 5f;

    public float criticalDamage = 50f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ChangeHealth(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnStatsChanged?.Invoke();
    }

    public void AddModifier(StatModifier modifier)
    {
        ApplyModifier(modifier, 1);
        OnStatsChanged?.Invoke();
    }

    public void RemoveModifier(StatModifier modifier)
    {
        ApplyModifier(modifier, -1);
        OnStatsChanged?.Invoke();
    }

    private void ApplyModifier(StatModifier modifier, int multiplier)
    {
        int value = modifier.amount * multiplier;

        switch (modifier.stat)
        {
            case StatType.Strength:
                strength += value;
                break;

            case StatType.Defense:
                defense += value;
                break;

            case StatType.Agility:
                agility += value;
                break;

            case StatType.Intelligence:
                intelligence += value;
                break;

            case StatType.Damage:
                damage += value;
                break;

            case StatType.MoveSpeed:
                moveSpeed += value;
                break;

            case StatType.MaxHealth:
                maxHealth += value;

                currentHealth = Mathf.Clamp(
                    currentHealth + value,
                    0,
                    maxHealth);

                break;

            case StatType.CriticalChance:
                criticalChance += value;
                break;

            case StatType.CriticalDamage:
                criticalDamage += value;
                break;
        }
    }
}