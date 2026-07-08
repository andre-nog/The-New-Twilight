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

    // Image.fillAmount em vez de Slider — Slider.set_maxValue dispara um rebuild de
    // layout via SendMessage internamente, o que gera "SendMessage cannot be called
    // during Awake/OnValidate" quando UpdateUI() é chamado a partir de
    // StatsManager.OnValidate (edição de "level" no Inspector). Image.fillAmount não
    // tem esse problema.
    public Image expFillImage;
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

    // OnMonsterDefeated agora carrega o EnemyArchetypeSO (pra QuestManager filtrar
    // por tipo de inimigo) — GainExperience não precisa dele, daí o lambda.
    private Enemy_Health.MonsterDefeated onMonsterDefeatedHandler;

    private void OnEnable()
    {
        onMonsterDefeatedHandler = (exp, _) => GainExperience(exp);
        Enemy_Health.OnMonsterDefeated += onMonsterDefeatedHandler;

        // Cobre level ups que não passam por GainExperience — edição manual de
        // "level" no Inspector (StatsManager.OnValidate) ou SetLevel no carregamento
        // de save. Sem isso, o texto "Level: N" só atualizava na próxima kill.
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnLevelChanged += UpdateUI;
    }

    private void OnDisable()
    {
        Enemy_Health.OnMonsterDefeated -= onMonsterDefeatedHandler;

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
        if (expFillImage != null)
            expFillImage.fillAmount = ExpToLevel > 0 ? (float)currentExp / ExpToLevel : 0f;

        if (currentLevelText != null && StatsManager.Instance != null)
            currentLevelText.text = "Level: " + StatsManager.Instance.level;
    }

    // Contrato de save — mesmo padrão de GoldManager.GetState/ApplyState (um único
    // valor primitivo, sem DTO dedicado).
    public int GetState() => currentExp;

    public void ApplyState(int exp)
    {
        currentExp = exp;
        UpdateUI();
    }
}
