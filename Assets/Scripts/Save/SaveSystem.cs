using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Orquestrador do fluxo completo de Novo Jogo / Save / Load — separado do
// GameManager, que fica só com composição de mundo/cena (player, câmera, respawn,
// reload). SaveSystem não conhece Inventory/Gold/Equipment/etc. além da interface
// genérica ISaveParticipant; StatsManager (vitals/posição) fica fora do loop de
// propósito — ver comentário em LoadInto.
public static class SaveSystem
{
    // Chaves cujo participante precisa estar registrado pra um Save ser considerado
    // válido — gravar um save sem um desses seria perder progresso silenciosamente.
    // Sistemas novos (Skills, Talents, Pets...) começam FORA desta lista por padrão:
    // um save antigo sem essa entrada só deixa o sistema no estado de Novo Jogo, não
    // é motivo pra abortar o carregamento.
    private static readonly string[] RequiredSaveKeys =
    {
        "gold", "inventory", "equipment", "quests", "worldItems"
    };

    // Chamado pelo GameManager em todo scene load que não tem um slot pendente
    // (boot frio ou EnterWorld(null)) — nunca implícito via Awake/Start de cada
    // manager, sempre esta chamada explícita.
    public static void InitializeNewGame()
    {
        foreach (ISaveParticipant participant in SaveRegistry.OrderedParticipants())
            participant.InitializeNewGame();

        // SkillLoadout/SkillProgression são DontDestroyOnLoad — sobrevivem ao reload
        // que precede um Novo Jogo, então os slots AO VIVO de PlayerSkillManager (já
        // construídos no Awake() do player novo, com o loadout ANTIGO ainda presente
        // nesse instante) ficam desatualizados depois que os dois acabaram de ser
        // limpos acima. RebuildLoadout() força PlayerSkillManager a re-derivar do
        // zero a partir da classe (SkillLoadout.Populated já está false).
        GameManager game = GameManager.Instance;
        PlayerSkillManager skillManager = game != null && game.Player != null
            ? game.Player.GetComponent<PlayerSkillManager>()
            : null;

        if (skillManager != null)
            skillManager.RebuildLoadout();
    }

    public static void Save(string slot)
    {
        GameManager game = GameManager.Instance;

        if (game == null || game.Player == null || StatsManager.Instance == null || game.ExpManager == null)
        {
            Debug.LogError("SaveSystem: Save abortado — GameManager/Player/StatsManager/ExpManager ausente.");
            return;
        }

        foreach (string key in RequiredSaveKeys)
        {
            if (!SaveRegistry.Has(key))
            {
                Debug.LogError($"SaveSystem: Save abortado — participante obrigatório '{key}' não está registrado.");
                return;
            }
        }

        StatsSave stats = StatsManager.Instance.GetState();

        SaveData data = new()
        {
            sceneId = SceneManager.GetActiveScene().name,
            player = new PlayerSave
            {
                classId = stats.classId,
                level = stats.level,
                currentExp = game.ExpManager.GetState(),
                currentHealth = stats.currentHealth,
                currentMana = stats.currentMana,
                position = game.Player.position
            }
        };

        foreach (ISaveParticipant participant in SaveRegistry.OrderedParticipants())
        {
            data.entries.Add(new SaveEntry
            {
                key = participant.SaveKey,
                schemaVersion = participant.SchemaVersion,
                json = participant.CaptureState()
            });
        }

        SaveService.Save(slot, data);
        Debug.Log($"Jogo salvo (slot '{slot}').");
    }

    // Ordem importa: classe → level/exp (recalc já dentro de SetClass/SetLevel) →
    // loop genérico de participantes (equipamento antes de inventário por Order,
    // ver ISaveParticipant) → vitals (clampam contra o MaxHealth/MaxMana já
    // recalculado pelo equipamento) → posição. Vitals/posição são sempre os dois
    // últimos passos, hardcoded, fora do loop — pertencem à entidade Player, não a
    // um manager plugável, e todo sistema futuro que afete stat derivado (Talents,
    // Pets...) precisa ter rodado antes deles de qualquer forma.
    public static void LoadInto(string slot)
    {
        if (!SaveService.TryLoad(slot, out SaveData data))
            return;

        GameManager game = GameManager.Instance;

        if (game == null || StatsManager.Instance == null || game.Player == null)
            return;

        ClassDefinitionSO savedClass = game.FindClassById(data.player.classId);
        PlayerSkillManager skillManager = game.Player.GetComponent<PlayerSkillManager>();

        if (savedClass != null)
        {
            StatsManager.Instance.SetClass(savedClass);

            if (skillManager != null)
                skillManager.RebuildLoadout();
        }

        StatsManager.Instance.SetLevel(data.player.level);

        if (game.ExpManager != null)
            game.ExpManager.ApplyState(data.player.currentExp);

        foreach (ISaveParticipant participant in SaveRegistry.OrderedParticipants())
        {
            SaveEntry entry = FindEntry(data.entries, participant.SaveKey);

            if (entry != null)
                participant.RestoreState(entry.json, entry.schemaVersion);
        }

        // SkillLoadout.RestoreState só atualiza o dicionário do singleton — os
        // slots AO VIVO em PlayerSkillManager (já construídos por RebuildLoadout,
        // acima) precisam ser resincronizados pra refletir o loadout recém-restaurado.
        if (skillManager != null)
            skillManager.ResyncFromLoadout();

        StatsManager.Instance.SetVitals(data.player.currentHealth, data.player.currentMana);

        game.Player.position = data.player.position;

        Debug.Log($"Jogo carregado (slot '{slot}').");
    }

    private static SaveEntry FindEntry(List<SaveEntry> entries, string key)
    {
        foreach (SaveEntry entry in entries)
        {
            if (entry.key == key)
                return entry;
        }

        return null;
    }
}
