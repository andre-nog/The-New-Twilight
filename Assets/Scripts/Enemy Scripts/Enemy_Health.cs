using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(EnemyStats))]
public class Enemy_Health : MonoBehaviour, IDamageable
{
    public delegate void MonsterDefeated(int exp);
    public static event MonsterDefeated OnMonsterDefeated;

    // Registro de inimigos vivos na cena — evita FindGameObjectsWithTag em quem
    // precisa iterar todos (ex.: PlayerTargeting ao ciclar alvos com Tab).
    public static readonly List<Enemy_Health> Active = new();

    public int currentHealth;

    public Slider healthSlider;

    // maxHealth/armor/expReward saíram daqui — agora vêm do EnemyArchetypeSO via EnemyStats.
    private EnemyStats stats;

    public float Armor => stats.Armor;
    public bool IsAlive => currentHealth > 0;

    private void Awake()
    {
        stats = GetComponent<EnemyStats>();
    }

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    private void Start()
    {
        currentHealth = stats.MaxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = stats.MaxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void TakeDamage(DamageResult result)
    {
        ChangeHealth(-result.FinalDamage, result.IsCritical);
    }

    public void ChangeHealth(int amount, bool critical = false)
    {
        currentHealth += amount;

        currentHealth = Mathf.Clamp(currentHealth, 0, stats.MaxHealth);

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        // Só cria popup quando tomou dano
        if (amount < 0)
        {
            Color popupColor = critical
                ? new Color(1f, 0.85f, 0f) // Dourado
                : Color.white;

            DamageManager.Instance.CreatePopup(
                transform.position + Vector3.up * 0.5f,
                -amount,
                popupColor
            );
        }

        if (currentHealth <= 0)
        {
            OnMonsterDefeated?.Invoke(stats.ExpReward);
            Destroy(gameObject);
        }
    }

    public void ResetEnemy()
    {
        currentHealth = stats.MaxHealth;

        if (healthSlider != null)
            healthSlider.value = currentHealth;
    }
}
