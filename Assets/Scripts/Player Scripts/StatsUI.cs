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
        statsSlots[1].SetValue(StatsManager.Instance.strength.Total);
        statsSlots[2].SetValue(Mathf.RoundToInt(StatsManager.Instance.Armor));
        statsSlots[3].SetValue(StatsManager.Instance.agility.Total);
        statsSlots[4].SetValue(StatsManager.Instance.intelligence.Total);
        statsSlots[5].SetValue(Mathf.RoundToInt(StatsManager.Instance.AttackPower));
        statsSlots[6].SetValue(Mathf.RoundToInt(StatsManager.Instance.MoveSpeed));
    }
}