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
        statsSlots[0].SetValue(StatsManager.Instance.currentHealth);
        statsSlots[1].SetValue(StatsManager.Instance.strength);
        statsSlots[2].SetValue(StatsManager.Instance.defense);
        statsSlots[3].SetValue(StatsManager.Instance.agility);
        statsSlots[4].SetValue(StatsManager.Instance.intelligence);
        statsSlots[5].SetValue(StatsManager.Instance.damage);
        statsSlots[6].SetValue(StatsManager.Instance.moveSpeed);
    }
}