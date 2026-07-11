using System;
using System.Collections.Generic;
using UnityEngine;

// Cadeia de migração de save — cada versão antiga tem uma função que sabe
// reconstruir o SaveData atual a partir do JSON cru daquela versão. Adicionar uma
// nova versão-alvo (v4, v5...) = adicionar um novo case aqui; nunca mudar o schema
// atual (SaveData.cs) sem dar um caminho de migração pra quem já tem saves na
// versão anterior.
//
// v1->v2: QuestSave.state virou string. v2->v3: o schema plano (inventory/
// equipment/quests/gold/collectedWorldItems como campos nomeados) virou uma lista
// de SaveEntry opacos — ver SaveData.cs. v1 encadeia por v2 antes de chegar em v3.
public static class SaveMigrations
{
    public static bool TryMigrate(int fromVersion, string json, out SaveData migrated)
    {
        switch (fromVersion)
        {
            case 1:
            {
                SaveDataV2Legacy v2 = MigrateV1ToV2(json);
                migrated = v2 != null ? MigrateV2ToV3(v2) : null;
                return migrated != null;
            }

            case 2:
            {
                SaveDataV2Legacy legacy = ReadV2Legacy(json);
                migrated = legacy != null ? MigrateV2ToV3(legacy) : null;
                return migrated != null;
            }

            default:
                migrated = null;
                return false;
        }
    }

    // v1 -> v2 (legado): QuestSave.state era int (cast direto do enum QuestState,
    // frágil a reordenar o enum); v2 guarda o NOME do enum como string. Um valor
    // fora do range do enum (save corrompido/hand-edited) cai em QuestState.Available
    // com aviso, em vez de crashar ou corromper o resto do save.
    private static SaveDataV2Legacy MigrateV1ToV2(string json)
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

        SaveDataV2Legacy migrated = new()
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

    private static SaveDataV2Legacy ReadV2Legacy(string json)
    {
        try
        {
            return JsonUtility.FromJson<SaveDataV2Legacy>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveMigrations: falha ao ler save v2 pra migrar — {e.Message}");
            return null;
        }
    }

    // v2 -> v3: schema plano (campos nomeados por sistema) vira uma lista de
    // SaveEntry opacos, um por ISaveParticipant — ver SaveData.cs. Cada campo
    // antigo empacota no MESMO DTO que o manager correspondente usa hoje em
    // CaptureState/RestoreState (schemaVersion 1 — a forma "atual" de cada um no
    // momento em que esta migração foi escrita).
    private static SaveData MigrateV2ToV3(SaveDataV2Legacy old)
    {
        SaveData migrated = new()
        {
            sceneId = old.sceneId,
            player = old.player ?? new PlayerSave()
        };

        migrated.entries.Add(new SaveEntry
        {
            key = "gold",
            schemaVersion = 1,
            json = JsonUtility.ToJson(new GoldSave { gold = old.gold })
        });

        migrated.entries.Add(new SaveEntry
        {
            key = "inventory",
            schemaVersion = 1,
            json = JsonUtility.ToJson(new InventorySave { items = old.inventory ?? new List<ItemStackSave>() })
        });

        migrated.entries.Add(new SaveEntry
        {
            key = "equipment",
            schemaVersion = 1,
            json = JsonUtility.ToJson(new EquipmentSave { items = old.equipment ?? new List<EquippedSave>() })
        });

        migrated.entries.Add(new SaveEntry
        {
            key = "quests",
            schemaVersion = 1,
            json = JsonUtility.ToJson(new QuestsSave { quests = old.quests ?? new List<QuestSave>() })
        });

        migrated.entries.Add(new SaveEntry
        {
            key = "worldItems",
            schemaVersion = 1,
            json = JsonUtility.ToJson(new WorldItemsSave { ids = old.collectedWorldItems ?? new List<string>() })
        });

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

    // Formato exato do v2 — nunca editar, é o registro histórico do schema plano
    // (campos nomeados por sistema) de antes deste SaveData virar uma lista de
    // SaveEntry opacos.
    [Serializable]
    private class SaveDataV2Legacy
    {
        public int version;
        public string sceneId;
        public PlayerSave player = new();
        public List<ItemStackSave> inventory = new();
        public List<EquippedSave> equipment = new();
        public List<QuestSave> quests = new();
        public int gold;
        public List<string> collectedWorldItems = new();
    }
}
