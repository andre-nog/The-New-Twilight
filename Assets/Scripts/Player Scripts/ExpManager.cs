using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExpManager : MonoBehaviour
{
    public int currentExp;

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
    //
    // Curva fixa (não mais base/multiplicador configuráveis no Inspector):
    // xpToNextLevel = 100 + 50*(level-1) + 25*(level-1)^2.
    public int ExpToLevel
    {
        get
        {
            int level = StatsManager.Instance != null ? StatsManager.Instance.level : 1;
            int n = level - 1;

            return 100 + 50 * n + 25 * n * n;
        }
    }

    // OnMonsterDefeated agora carrega o EnemyArchetypeSO (pra QuestManager filtrar
    // por tipo de inimigo) e a posição da morte (pro popup de XP) — GainExperience
    // não precisa de nenhum dos dois, daí o lambda.
    private Enemy_Health.MonsterDefeated onMonsterDefeatedHandler;

    private static readonly Color XpPopupColor = new(0.4f, 0.85f, 1f);

    // Dano precisa aparecer primeiro e sozinho — o popup de XP espera um instante
    // antes de nascer pra nunca competir visualmente com o número de dano do
    // golpe que matou o inimigo (os dois nasceriam no mesmíssimo frame senão).
    private const float XpPopupDelay = 0.15f;

    private void OnEnable()
    {
        onMonsterDefeatedHandler = (exp, gold, archetype, position) =>
        {
            GainExperience(exp);
            StartCoroutine(ShowXpPopupDelayed(exp, position));
        };

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

    private IEnumerator ShowXpPopupDelayed(int exp, Vector3 position)
    {
        yield return new WaitForSeconds(XpPopupDelay);

        if (DamageManager.Instance != null)
            DamageManager.Instance.CreateRewardPopup(position + Vector3.up * 0.5f, $"+{exp} XP", XpPopupColor);
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
