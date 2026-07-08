using System;
using System.Collections.Generic;
using UnityEngine;

// Cadeia de migração de save — cada versão antiga tem uma função que sabe
// reconstruir o SaveData atual a partir do JSON cru daquela versão. Adicionar uma
// nova versão-alvo (v3, v4...) = adicionar um novo case aqui; nunca mudar o schema
// atual (SaveData.cs) sem dar um caminho de migração pra quem já tem saves na
// versão anterior. Só existe v1->v2 até agora (ver QuestSave.state).
public static class SaveMigrations
{
    public static bool TryMigrate(int fromVersion, string json, out SaveData migrated)
    {
        switch (fromVersion)
        {
            case 1:
                migrated = MigrateV1ToV2(json);
                return migrated != null;

            default:
                migrated = null;
                return false;
        }
    }

    // v1 -> v2: QuestSave.state era int (cast direto do enum QuestState, frágil a
    // reordenar o enum); v2 guarda o NOME do enum como string. Um valor fora do
    // range do enum (save corrompido/hand-edited) cai em QuestState.Available com
    // aviso, em vez de crashar ou corromper o resto do save.
    private static SaveData MigrateV1ToV2(string json)
    {
        SaveDataV1 old;

        try
        {
            old = JsonUtility.FromJson<SaveDataV1>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveMigrations: falha ao ler save v1 pra migrar — {e.Message}");
            return null;
        }

        if (old == null)
            return null;

        SaveData migrated = new()
        {
            sceneId = old.sceneId,
            player = old.player,
            inventory = old.inventory,
            equipment = old.equipment,
            gold = old.gold,
            quests = new List<QuestSave>()
        };

        if (old.quests != null)
        {
            foreach (QuestSaveV1 oldQuest in old.quests)
            {
                string stateName;

                if (Enum.IsDefined(typeof(QuestState), oldQuest.state))
                {
                    stateName = ((QuestState)oldQuest.state).ToString();
                }
                else
                {
                    Debug.LogWarning($"SaveMigrations: estado inteiro {oldQuest.state} da quest '{oldQuest.questId}' fora do enum QuestState — migrando pra Available.");
                    stateName = QuestState.Available.ToString();
                }

                migrated.quests.Add(new QuestSave
                {
                    questId = oldQuest.questId,
                    state = stateName,
                    progress = oldQuest.progress
                });
            }
        }

        return migrated;
    }

    // Formato exato do v1 — nunca editar, é um registro histórico do schema antigo
    // pra JsonUtility conseguir desserializar saves gravados antes da v2 existir.
    [Serializable]
    private class SaveDataV1
    {
        public int version;
        public string sceneId;
        public PlayerSave player = new();
        public List<ItemStackSave> inventory = new();
        public List<EquippedSave> equipment = new();
        public List<QuestSaveV1> quests = new();
        public int gold;
    }

    [Serializable]
    private class QuestSaveV1
    {
        public string questId;
        public int state;
        public int progress;
    }
}
