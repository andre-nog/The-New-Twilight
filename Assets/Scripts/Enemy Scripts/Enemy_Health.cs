using UnityEngine;
using UnityEngine.UI;

public class Enemy_Health : MonoBehaviour
{
    public int expReward = 3;

    public delegate void MonsterDefeated(int exp);
    public static event MonsterDefeated OnMonsterDefeated;

    public int maxHealth = 100;
    public int currentHealth;

    [Tooltip("Mitigação de dano recebido — a fórmula fica centralizada em DamageCalculator, não aqui.")]
    public float armor = 0f;

    public Slider healthSlider;

    private void Start()
    {
        currentHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void ChangeHealth(int amount, bool critical = false)
    {
        currentHealth += amount;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

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
            OnMonsterDefeated?.Invoke(expReward);
            Destroy(gameObject);
        }
    }

    public void ResetEnemy()
    {
        currentHealth = maxHealth;

        if (healthSlider != null)
            healthSlider.value = currentHealth;
    }
}