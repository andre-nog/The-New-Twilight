using System;
using System.IO;
using UnityEngine;

// Persistência mínima: um único save.json versionado em Application.persistentDataPath.
// Sem dicionários/nested references — JsonUtility não suporta polimorfismo nem
// referências compartilhadas, então SaveData é só uma árvore de DTOs simples.
public static class SaveService
{
    private const int CurrentVersion = 1;

    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save(SaveData data)
    {
        data.version = CurrentVersion;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }

    public static bool TryLoad(out SaveData data)
    {
        data = null;

        if (!File.Exists(SavePath))
            return false;

        SaveData loaded;

        try
        {
            loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveService: falha ao ler save.json — {e.Message}");
            return false;
        }

        if (loaded == null)
            return false;

        // Gate de versão — hoje só existe v1, então qualquer coisa diferente é
        // rejeitada em vez de aplicada pela metade. Migrações entram aqui como
        // switch (loaded.version) quando a v2 existir.
        if (loaded.version != CurrentVersion)
        {
            Debug.LogWarning($"SaveService: save.json com versão {loaded.version} incompatível (esperado {CurrentVersion}), ignorando.");
            return false;
        }

        data = loaded;
        return true;
    }

    public static void Delete()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }
}
