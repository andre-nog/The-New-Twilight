using UnityEngine;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public TMP_Text healthText;

    private void OnEnable()
    {
        StatsManager.Instance.OnStatsChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnStatsChanged -= RefreshUI;
    }

    public void ChangeHealth(int amount)
    {
        StatsManager.Instance.ChangeHealth(amount);
    }

    private void RefreshUI()
    {
        healthText.text = "HP: " + StatsManager.Instance.currentHealth + " / " + StatsManager.Instance.maxHealth;

        if (StatsManager.Instance.currentHealth <= 0)
            gameObject.SetActive(false);
    }
}
