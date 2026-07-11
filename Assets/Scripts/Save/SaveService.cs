using System;
using System.IO;
using UnityEngine;

// Persistência multi-slot: um arquivo JSON versionado por slot em
// Application.persistentDataPath, nomeado save_<slot>.json. Sem dicionários/nested
// references — JsonUtility não suporta polimorfismo nem referências compartilhadas,
// então SaveData é só uma árvore de DTOs simples.
public static class SaveService
{
    private const int CurrentVersion = 3;

    // 3 slots manuais numerados + 1 autosave, dedicados — nunca se sobrescrevem entre
    // si. "Debug" é só pro atalho de quicksave/quickload do Editor (GameManager F5/F9),
    // separado dos slots "reais" pra iteração rápida de balanceamento nunca pisar em
    // progresso salvo deliberadamente.
    public const string ManualSlot1 = "slot1";
    public const string ManualSlot2 = "slot2";
    public const string ManualSlot3 = "slot3";
    public const string AutosaveSlot = "autosave";
    public const string DebugSlot = "debug";

    public static readonly string[] ManualSlots = { ManualSlot1, ManualSlot2, ManualSlot3 };

    private static string SavePath(string slot) => Path.Combine(Application.persistentDataPath, $"save_{slot}.json");
    private static string BackupPath(string slot) => Path.Combine(Application.persistentDataPath, $"save_{slot}.json.bak");
    private static string TempPath(string slot) => Path.Combine(Application.persistentDataPath, $"save_{slot}.json.tmp");

    // Escrita atômica: grava num arquivo temporário primeiro, sobe o save atual (se
    // existir) pra .bak, só então move o temp pro lugar do save real. File.Move é
    // uma renomeação (atômica no mesmo volume), não uma cópia byte-a-byte — se o
    // processo morrer no meio, o pior caso é o save antigo continuar intacto (a
    // mudança mais recente se perde, mas nunca a única cópia).
    public static void Save(string slot, SaveData data)
    {
        data.version = CurrentVersion;

        string json = JsonUtility.ToJson(data, true);
        string savePath = SavePath(slot);
        string backupPath = BackupPath(slot);
        string tempPath = TempPath(slot);

        try
        {
            File.WriteAllText(tempPath, json);

            if (File.Exists(savePath))
            {
                if (File.Exists(backupPath))
                    File.Delete(backupPath);

                File.Move(savePath, backupPath);
            }

            File.Move(tempPath, savePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveService: falha ao salvar o slot '{slot}' — {e.Message}");
        }
    }

    public static bool TryLoad(string slot, out SaveData data)
    {
        if (TryLoadFrom(SavePath(slot), out data))
            return true;

        Debug.LogWarning($"SaveService: save do slot '{slot}' ausente/corrompido/incompatível — tentando o backup.");

        if (TryLoadFrom(BackupPath(slot), out data))
            return true;

        Debug.LogError($"SaveService: nenhum save válido encontrado pro slot '{slot}' (nem o principal, nem o backup).");
        return false;
    }

    // Pra uma futura UI de seleção de slot mostrar quais já têm progresso salvo.
    public static bool SlotExists(string slot) => File.Exists(SavePath(slot)) || File.Exists(BackupPath(slot));

    [Serializable]
    private class VersionProbe
    {
        public int version;
    }

    private static bool TryLoadFrom(string path, out SaveData data)
    {
        data = null;

        if (!File.Exists(path))
            return false;

        string json;

        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveService: falha ao ler {Path.GetFileName(path)} — {e.Message}");
            return false;
        }

        // Lê só o campo version primeiro (JsonUtility ignora o resto dos campos que
        // VersionProbe não declara) — decide ANTES de tentar desserializar pro
        // SaveData atual, porque um save de versão antiga com um campo de tipo
        // diferente (ex.: QuestSave.state era int, agora é string) desserializaria
        // esse campo específico como default silenciosamente se eu tentasse direto.
        VersionProbe probe;

        try
        {
            probe = JsonUtility.FromJson<VersionProbe>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveService: falha ao ler a versão de {Path.GetFileName(path)} — {e.Message}");
            return false;
        }

        if (probe == null)
            return false;

        if (probe.version == CurrentVersion)
        {
            SaveData loaded;

            try
            {
                loaded = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveService: falha ao ler {Path.GetFileName(path)} — {e.Message}");
                return false;
            }

            if (loaded == null)
                return false;

            data = loaded;
            return true;
        }

        if (SaveMigrations.TryMigrate(probe.version, json, out SaveData migrated))
        {
            Debug.LogWarning($"SaveService: {Path.GetFileName(path)} migrado da versão {probe.version} para {CurrentVersion}.");
            data = migrated;
            return true;
        }

        Debug.LogWarning($"SaveService: {Path.GetFileName(path)} com versão {probe.version} incompatível (esperado {CurrentVersion}) e sem migração disponível — ignorando.");
        return false;
    }

    public static void Delete(string slot)
    {
        string savePath = SavePath(slot);
        string backupPath = BackupPath(slot);
        string tempPath = TempPath(slot);

        if (File.Exists(savePath))
            File.Delete(savePath);

        if (File.Exists(backupPath))
            File.Delete(backupPath);

        if (File.Exists(tempPath))
            File.Delete(tempPath);
    }
}
