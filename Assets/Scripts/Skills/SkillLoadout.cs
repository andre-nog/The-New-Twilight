using System;
using System.Collections.Generic;
using UnityEngine;

// DTO de save deste manager — ver ISaveParticipant/SaveSystem. Ícone não é salvo
// (deriva de Skill.icon na hora de aplicar) — não é meaningfully serializável via
// JsonUtility e seria redundante com o que Skill.icon já guarda.
[Serializable]
public class SkillLoadoutSave
{
    public List<SkillSlotEntry> slots = new();
}

[Serializable]
public class SkillSlotEntry
{
    public int index;
    public string skillId;
}

// Estado persistente do arranjo da barra de skills: qual skill está em cada
// índice de slot (o ícone nunca é guardado aqui — vem sempre de Skill.icon, lido
// direto na hora de exibir). Mesmo padrão de SkillProgression — singleton runtime
// com DontDestroyOnLoad, então sobrevive ao respawn do player (destroy+recreate).
//
// Sem isto, PlayerSkillManager.EnsureSlotsBuilt recomputava os slots do zero a
// partir de ClassDefinitionSO.defaultSkills a cada respawn, perdendo qualquer
// skill que o jogador tivesse arrastado do Livro pra um slot fora do kit padrão.
public class SkillLoadout : MonoBehaviour, ISaveParticipant
{
    public string SaveKey => "skills.loadout";
    public int SchemaVersion => 1;
    public int Order => 0;

    public static SkillLoadout Instance { get; private set; }

    private readonly Dictionary<int, Skill> skills = new();

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

        SaveRegistry.Register(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SaveRegistry.Unregister(this);
    }

    public Skill GetSkill(int index)
    {
        return skills.TryGetValue(index, out Skill skill) ? skill : null;
    }

    public void Set(int index, Skill skill)
    {
        skills[index] = skill;
        Populated = true;
    }

    // Chamado quando a classe muda (promoção) — o kit antigo não faz mais sentido,
    // então força PlayerSkillManager a re-derivar o layout inicial da classe nova.
    public void Clear()
    {
        skills.Clear();
        Populated = false;
    }

    // Novo Jogo = barra derivada do zero pela classe (PlayerSkillManager cuida
    // disso quando Populated é false). Chamado só pelo SaveSystem.
    public void InitializeNewGame()
    {
        Clear();
    }

    public string CaptureState()
    {
        List<SkillSlotEntry> entries = new();

        foreach (KeyValuePair<int, Skill> entry in skills)
        {
            if (entry.Value == null)
                continue;

            if (string.IsNullOrEmpty(entry.Value.Id))
            {
                Debug.LogWarning($"SkillLoadout: skill '{entry.Value.skillName}' (slot {entry.Key}) tem Id vazio — não será salva. Reimporte o asset ou abra-o no Inspector pra gerar o id.");
                continue;
            }

            entries.Add(new SkillSlotEntry { index = entry.Key, skillId = entry.Value.Id });
        }

        return JsonUtility.ToJson(new SkillLoadoutSave { slots = entries });
    }

    // Só atualiza os dicionários deste singleton — PlayerSkillManager.ResyncFromLoadout()
    // (chamado pelo SaveSystem logo em seguida) reflete isso nos slots ao vivo da
    // barra e na UI.
    public void RestoreState(string json, int schemaVersion)
    {
        Clear();

        SkillLoadoutSave save = JsonUtility.FromJson<SkillLoadoutSave>(json);

        if (save?.slots == null)
            return;

        foreach (SkillSlotEntry saved in save.slots)
        {
            Skill skill = SkillProgression.FindSkillById(saved.skillId);

            if (skill == null)
            {
                Debug.LogWarning($"SkillLoadout: skillId '{saved.skillId}' (slot {saved.index}) não encontrado no roster da classe atual — descartado.");
                continue;
            }

            Set(saved.index, skill);
        }
    }
}
