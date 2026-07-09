using System.Collections.Generic;
using UnityEngine;

// Estado persistente do arranjo da barra de skills: qual skill (e ícone) está em
// cada índice de slot. Mesmo padrão de SkillProgression — singleton runtime com
// DontDestroyOnLoad, então sobrevive ao respawn do player (destroy+recreate).
//
// Sem isto, PlayerSkillManager.EnsureSlotsBuilt recomputava os slots do zero a
// partir de ClassDefinitionSO.defaultSkills a cada respawn, perdendo qualquer
// skill que o jogador tivesse arrastado do Livro pra um slot fora do kit padrão.
public class SkillLoadout : MonoBehaviour
{
    public static SkillLoadout Instance { get; private set; }

    private readonly Dictionary<int, Skill> skills = new();
    private readonly Dictionary<int, Sprite> icons = new();

    // False só antes do primeiríssimo EnsureSlotsBuilt da sessão — nesse caso
    // PlayerSkillManager ainda deriva o layout inicial da classe. Depois disso, o
    // loadout vira a única fonte de verdade (mesmo pra slots vazios).
    public bool Populated { get; private set; }

    public static void EnsureCreated()
    {
        if (Instance != null)
            return;

        GameObject host = new("Skill Loadout");
        host.AddComponent<SkillLoadout>();
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
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public Skill GetSkill(int index)
    {
        return skills.TryGetValue(index, out Skill skill) ? skill : null;
    }

    public Sprite GetIcon(int index)
    {
        return icons.TryGetValue(index, out Sprite icon) ? icon : null;
    }

    public void Set(int index, Skill skill, Sprite icon)
    {
        skills[index] = skill;
        icons[index] = icon;
        Populated = true;
    }

    // Chamado quando a classe muda (promoção) — o kit antigo não faz mais sentido,
    // então força PlayerSkillManager a re-derivar o layout inicial da classe nova.
    public void Clear()
    {
        skills.Clear();
        icons.Clear();
        Populated = false;
    }
}
