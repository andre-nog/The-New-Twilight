using System;
using System.Collections.Generic;
using UnityEngine;

// DTO de save deste manager — ver ISaveParticipant/SaveSystem.
[Serializable]
public class WorldItemsSave
{
    public List<string> ids = new();
}

// Registro mínimo de "delta de mundo": quais WorldItem (Item.cs) já foram
// coletados nesta partida. Sem isso, um reload de cena (EnterWorld) recria TODO
// objeto scene-local do zero a partir do arquivo da cena — incluindo itens
// autorados que o jogador já pegou, duplicando-os (o item já está no inventário
// salvo E reaparece no chão). Só guarda identidade (o id do item já coletado),
// nunca estado derivado — mesmo princípio do resto do save.
//
// Scene-local (não DontDestroyOnLoad), auto-criado via EnsureCreated() no
// bootstrap do GameManager — igual SkillBarUI/InventoryDragController: um reload
// destrói a instância antiga (limpando collectedIds) e uma nova nasce vazia, pronta
// pra ser repopulada por ApplyState quando o save for aplicado.
public class WorldItemRegistry : MonoBehaviour, ISaveParticipant
{
    public string SaveKey => "worldItems";
    public int SchemaVersion => 1;
    public int Order => 0;

    public static WorldItemRegistry Instance { get; private set; }

    private readonly HashSet<string> collectedIds = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SaveRegistry.Register(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SaveRegistry.Unregister(this);
    }

    public static void EnsureCreated()
    {
        if (Instance != null)
            return;

        new GameObject("World Item Registry").AddComponent<WorldItemRegistry>();
    }

    public bool IsCollected(string worldItemId)
    {
        return !string.IsNullOrEmpty(worldItemId) && collectedIds.Contains(worldItemId);
    }

    public void MarkCollected(string worldItemId)
    {
        if (!string.IsNullOrEmpty(worldItemId))
            collectedIds.Add(worldItemId);
    }

    // Novo Jogo = nenhum WorldItem coletado ainda. Chamado só pelo SaveSystem.
    public void InitializeNewGame()
    {
        collectedIds.Clear();
    }

    public string CaptureState()
    {
        return JsonUtility.ToJson(new WorldItemsSave { ids = new List<string>(collectedIds) });
    }

    public void RestoreState(string json, int schemaVersion)
    {
        collectedIds.Clear();

        WorldItemsSave save = JsonUtility.FromJson<WorldItemsSave>(json);

        if (save?.ids != null)
            collectedIds.UnionWith(save.ids);
    }
}
