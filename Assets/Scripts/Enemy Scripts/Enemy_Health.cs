using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(EnemyStats))]
public class Enemy_Health : MonoBehaviour, IDamageable
{
    // archetype (não mais displayName) — QuestManager comparava por string exata
    // contra EnemyStats.DisplayName, frágil a rename/typo. Referência direta ao
    // asset nunca dessincroniza.
    public delegate void MonsterDefeated(int exp, int gold, EnemyArchetypeSO archetype, Vector3 position);
    public static event MonsterDefeated OnMonsterDefeated;

    // Registro de inimigos vivos na cena — evita FindGameObjectsWithTag em quem
    // precisa iterar todos (ex.: PlayerTargeting ao ciclar alvos com Tab).
    public static readonly List<Enemy_Health> Active = new();

    public int currentHealth;

    // Encontrado em Awake() via GetComponentInChildren — nenhum prefab precisa mais
    // arrastar isso manualmente, contanto que siga a hierarquia padrão (Canvas >
    // HealthBar com um Slider) do prefab-template.
    private Slider healthSlider;

    // maxHealth/armor/expReward saíram daqui — agora vêm do EnemyArchetypeSO via EnemyStats.
    private EnemyStats stats;
    private EnemyRespawn respawn;
    private bool isDead;

    public float Armor => stats.Armor;
    public bool IsAlive => currentHealth > 0;

    // Cacheado uma vez em vez de GetComponent a cada poll do CombatStateTracker.
    public Enemy_Movement Movement { get; private set; }

    private void Awake()
    {
        stats = GetComponent<EnemyStats>();
        Movement = GetComponent<Enemy_Movement>();
        respawn = GetComponent<EnemyRespawn>();
        healthSlider = GetComponentInChildren<Slider>();
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

            Vector3 damageOffset = stats.Archetype != null ? stats.Archetype.damageTextOffset : Vector3.up * 0.5f;

            DamageManager.Instance.CreatePopup(
                transform.position + damageOffset,
                -amount,
                popupColor
            );

            // PlayClipAtPoint (não PlayOneShot num AudioSource do próprio inimigo):
            // um golpe fatal destrói este GameObject no mesmo frame, cortando um
            // PlayOneShot antes de tocar. PlayClipAtPoint cria um objeto temporário
            // independente que sobrevive à destruição do inimigo.
            if (stats.Archetype != null && stats.Archetype.hitSfx != null)
                AudioSource.PlayClipAtPoint(stats.Archetype.hitSfx, transform.position);
        }

        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;

            if (stats.Archetype != null && stats.Archetype.deathSfx != null)
                AudioSource.PlayClipAtPoint(stats.Archetype.deathSfx, transform.position);

            OnMonsterDefeated?.Invoke(stats.ExpReward, stats.GoldReward, stats.Archetype, transform.position);
            respawn?.ScheduleRespawn();
            Destroy(gameObject);
        }
    }

    public void ResetEnemy()
    {
        isDead = false;
        currentHealth = stats.MaxHealth;

        if (healthSlider != null)
            healthSlider.value = currentHealth;
    }
}
