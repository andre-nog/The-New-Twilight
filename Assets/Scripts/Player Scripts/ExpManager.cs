using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

public class ExpManager : MonoBehaviour
{
    public int currentExp;

    [Tooltip("Experiência necessária para sair do nível 1. Os níveis seguintes são derivados: baseExpToLevel * expGrowthMultiplier^(level-1).")]
    [FormerlySerializedAs("expToLevel")]
    public int baseExpToLevel = 10;
    public float expGrowthMultiplier = 1.2f;

    public Slider expSlider;
    public TMP_Text currentLevelText;

    // Derivado do nível atual em vez de acumulado a cada level up — assim não há
    // como dessincronizar do StatsManager.level (que é editável no Inspector) e o
    // save só precisa guardar level + currentExp.
    public int ExpToLevel
    {
        get
        {
            int level = StatsManager.Instance != null ? StatsManager.Instance.level : 1;
            return Mathf.RoundToInt(baseExpToLevel * Mathf.Pow(expGrowthMultiplier, level - 1));
        }
    }

    private void OnEnable()
    {
        Enemy_Health.OnMonsterDefeated += GainExperience;

        // Cobre level ups que não passam por GainExperience — edição manual de
        // "level" no Inspector (StatsManager.OnValidate) ou SetLevel no carregamento
        // de save. Sem isso, o texto "Level: N" só atualizava na próxima kill.
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnLevelChanged += UpdateUI;
    }

    private void OnDisable()
    {
        Enemy_Health.OnMonsterDefeated -= GainExperience;

        if (StatsManager.Instance != null)
            StatsManager.Instance.OnLevelChanged -= UpdateUI;
    }

    private void Start()
    {
        UpdateUI();
    }

    public void GainExperience(int amount)
    {
        currentExp += amount;

        while (currentExp >= ExpToLevel)
        {
            LevelUp();
        }

        UpdateUI();
    }

    private void LevelUp()
    {
        currentExp -= ExpToLevel;

        StatsManager.Instance.OnLevelUp();
    }

    public void UpdateUI()
    {
        if (expSlider != null)
        {
            expSlider.maxValue = ExpToLevel;
            expSlider.value = currentExp;
        }

        if (currentLevelText != null && StatsManager.Instance != null)
            currentLevelText.text = "Level: " + StatsManager.Instance.level;
    }
}
