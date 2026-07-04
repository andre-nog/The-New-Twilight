using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Lookup id -> ItemSO usado pelo save/load (o save guarda só o id, nunca a
// referência direta). Populado no Editor via "Coletar itens do projeto" — em
// build, é só uma lista serializada, sem Resources.Load nem AssetDatabase.
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabaseSO : ScriptableObject
{
    [SerializeField] private List<ItemSO> items = new();

    private Dictionary<string, ItemSO> lookup;

    public ItemSO GetById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        lookup ??= BuildLookup();

        return lookup.TryGetValue(id, out ItemSO item) ? item : null;
    }

    private Dictionary<string, ItemSO> BuildLookup()
    {
        Dictionary<string, ItemSO> result = new();

        foreach (ItemSO item in items)
        {
            if (item != null && !string.IsNullOrEmpty(item.Id))
                result[item.Id] = item;
        }

        return result;
    }

#if UNITY_EDITOR
    [ContextMenu("Coletar itens do projeto")]
    private void CollectItemsFromProject()
    {
        items.Clear();

        foreach (string guid in AssetDatabase.FindAssets("t:ItemSO"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemSO item = AssetDatabase.LoadAssetAtPath<ItemSO>(path);

            if (item == null)
                continue;

            item.EnsureId();
            items.Add(item);
        }

        lookup = null;
        EditorUtility.SetDirty(this);
    }
#endif
}
