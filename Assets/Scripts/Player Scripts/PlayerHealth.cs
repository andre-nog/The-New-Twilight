using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public TMP_Text healthText;

    private void Start()
    {
        healthText.text = "HP: " + StatsManager.Instance.currentHealth + " / " + StatsManager.Instance.maxHealth;
    }

    private void Update()
    {
        healthText.text = "HP: " + StatsManager.Instance.currentHealth + " / " + StatsManager.Instance.maxHealth;

        if (StatsManager.Instance.currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    public void ChangeHealth(int amount)
    {
        StatsManager.Instance.currentHealth += amount;
        healthText.text = "HP: " + StatsManager.Instance.currentHealth + " / " + StatsManager.Instance.maxHealth;

        if (StatsManager.Instance.currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}