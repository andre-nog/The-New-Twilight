using UnityEngine;

public class StatsUI : MonoBehaviour
{
    [SerializeField]
    private StatsSlot[] statsSlots;

    private void OnEnable()
    {
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.OnStatsChanged += UpdateAllStats;
        }
        else
        {
            Debug.LogWarning("StatsUI: StatsManager.Instance era null no OnEnable."); // NOVO
        }
    }

    private void Start()
    {
        UpdateAllStats();
    }

    private void OnDisable()
    {
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.OnStatsChanged -= UpdateAllStats;
        }
    }

    public void UpdateAllStats()
    {
        StatsManager stats = StatsManager.Instance;

        if (stats == null)
            return;

        // O array é preenchido no Inspector — se estiver menor que o esperado (slot
        // ainda não criado/atribuído), atualiza o que der em vez de estourar.
        int[] values =
        {
            stats.currentHealth,
            stats.strength.Total,
            Mathf.RoundToInt(stats.Armor),
            stats.agility.Total,
            stats.intelligence.Total,
            Mathf.RoundToInt(stats.AttackPower),
            Mathf.RoundToInt(stats.MoveSpeed)
        };

        int count = Mathf.Min(values.Length, statsSlots != null ? statsSlots.Length : 0);

        for (int i = 0; i < count; i++)
        {
            if (statsSlots[i] != null)
                statsSlots[i].SetValue(values[i]);
        }
    }
}