using System;
using System.Collections.Generic;
using UnityEngine;

// Estado de progressão de skills do jogador: qual nível cada skill está e quantos
// pontos há pra gastar. Singleton persistente criado em runtime (mesmo padrão de
// SkillBarUI/SkillDragController.EnsureCreated) — vive num GameObject próprio com
// DontDestroyOnLoad, então sobrevive ao respawn do player (o nível das skills é
// estado de "conta", não do objeto que morre e é reinstanciado).
//
// O roster (quais skills existem pra aprender) vem de
// StatsManager.CurrentClass.learnableSkills, então trocar de classe troca o leque
// de skills junto — sem re-wiring no Inspector.
public class SkillProgression : MonoBehaviour
{
    public static SkillProgression Instance { get; private set; }

    public event Action OnProgressionChanged;

    // skill -> nível atual (0 = não aprendida). Só skills do roster entram aqui.
    private readonly Dictionary<Skill, int> levels = new();

    public static void EnsureCreated()
    {
        if (Instance != null)
            return;

        GameObject host = new("Skill Progression");
        host.AddComponent<SkillProgression>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildRoster();

        if (StatsManager.Instance != null)
            StatsManager.Instance.OnLevelChanged += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnLevelChanged -= HandleLevelUp;

        if (Instance == this)
            Instance = null;
    }

    // Popula o dicionário a partir do roster da classe atual. Skills autoLearnedAtStart
    // nascem no nível 1 (de graça); as demais no nível 0.
    //
    // União de learnableSkills + defaultSkills (não só learnableSkills): defaultSkills
    // já precisa conter a skill autoLearnedAtStart pra ela aparecer na barra desde o
    // início (ver PlayerSkillManager), então cobrir as duas listas evita que a skill
    // fique "esquecida" no nível 0 só porque não foi replicada em learnableSkills.
    private void BuildRoster()
    {
        levels.Clear();

        ClassDefinitionSO currentClass = StatsManager.Instance != null
            ? StatsManager.Instance.CurrentClass
            : null;

        if (currentClass == null)
            return;

        AddToRoster(currentClass.defaultSkills);
        AddToRoster(currentClass.learnableSkills);
    }

    private void AddToRoster(List<Skill> skills)
    {
        if (skills == null)
            return;

        foreach (Skill skill in skills)
        {
            if (skill == null || levels.ContainsKey(skill))
                continue;

            levels[skill] = skill.autoLearnedAtStart ? 1 : 0;
        }
    }

    // Chamado quando a classe muda (promoção/carregamento de save) — re-deriva o
    // roster a partir da nova classe. Zera níveis pro padrão da classe nova (as
    // autoLearnedAtStart voltam pro nível 1); pontos são derivados do nível, então
    // se reajustam sozinhos.
    public void RebuildRoster()
    {
        BuildRoster();
        OnProgressionChanged?.Invoke();
    }

    private void HandleLevelUp()
    {
        // AvailablePoints é derivado do nível, então não há nada a incrementar aqui —
        // só avisar quem escuta (o Livro) que o estado mudou (ponto novo, talvez um
        // upgrade que passou a ser permitido).
        OnProgressionChanged?.Invoke();
    }

    public int GetLevel(Skill skill)
    {
        return skill != null && levels.TryGetValue(skill, out int level) ? level : 0;
    }

    public bool IsLearned(Skill skill) => GetLevel(skill) >= 1;

    // Dados efetivos do nível em que a skill está (mínimo 1 pra não-aprendidas, já que
    // o combate só chega aqui pra skills aprendidas).
    public SkillLevelData GetActiveData(Skill skill)
    {
        return skill != null ? skill.GetLevelData(Mathf.Max(1, GetLevel(skill))) : null;
    }

    // Pontos já gastos: cada nível acima da linha de base (as autoLearnedAtStart ganham
    // o nível 1 de graça, então não conta) custou 1 ponto.
    public int SpentPoints
    {
        get
        {
            int spent = 0;

            foreach (KeyValuePair<Skill, int> entry in levels)
            {
                int freeBaseline = entry.Key.autoLearnedAtStart ? 1 : 0;
                spent += Mathf.Max(0, entry.Value - freeBaseline);
            }

            return spent;
        }
    }

    // 1 ponto por nível a partir do 2 (o nível 1 não dá ponto). Derivado do nível atual
    // menos o que já foi gasto — assim gastar um ponto (subir uma skill) já desconta
    // sozinho, e mexer no level pelo Inspector pra testar re-concede os pontos.
    public int AvailablePoints
    {
        get
        {
            int playerLevel = StatsManager.Instance != null ? StatsManager.Instance.level : 1;
            return Mathf.Max(0, (playerLevel - 1) - SpentPoints);
        }
    }

    public bool CanLearnOrUpgrade(Skill skill)
    {
        if (skill == null)
            return false;

        int current = GetLevel(skill);

        if (current >= skill.MaxLevel)
            return false; // já no máximo

        if (AvailablePoints <= 0)
            return false; // sem pontos

        int playerLevel = StatsManager.Instance != null ? StatsManager.Instance.level : 1;
        int required = skill.GetLevelData(current + 1).requiredPlayerLevel;

        return playerLevel >= required;
    }

    public bool LearnOrUpgrade(Skill skill)
    {
        if (!CanLearnOrUpgrade(skill))
            return false;

        levels[skill] = GetLevel(skill) + 1;
        OnProgressionChanged?.Invoke();
        return true;
    }

    // Atalhos estáticos null-safe pro combate/cooldown, que só têm a Skill em mãos.
    public static int LevelOf(Skill skill)
    {
        return Instance != null ? Instance.GetLevel(skill) : 0;
    }

    public static SkillLevelData DataFor(Skill skill)
    {
        if (skill == null)
            return null;

        return Instance != null ? Instance.GetActiveData(skill) : skill.GetLevelData(1);
    }
}
