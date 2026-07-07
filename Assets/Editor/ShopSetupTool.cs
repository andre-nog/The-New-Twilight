using UnityEditor;
using UnityEngine;

// Espelha QuestGiverSetupTool: cuida da parte de serializar à mão no "NPC Shop"
// já existente na cena — tag/layer "NPC" (mesmo pipeline de hover/click-to-walk
// do PlayerInteraction via IInteractable), BoxCollider2D de trigger,
// ShopInteractable, e o placeholder de saco de compras acima da cabeça
// (SpriteRenderer simples, sprite atribuído depois direto no Inspector).
public static class ShopSetupTool
{
    private const string ShopNpcName = "NPC Shop";
    private const string NpcLayerName = "NPC";

    // GoldManager é um singleton de cena como CancelManager/GameManager/QuestManager
    // (cada um seu próprio GameObject top-level) — cria o dele se ainda não existir.
    [MenuItem("Tools/Shop/Setup Gold Manager")]
    private static void SetupGoldManager()
    {
        GoldManager existing = Object.FindAnyObjectByType<GoldManager>();

        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        GameObject goldManagerObject = new("GoldManager");
        Undo.RegisterCreatedObjectUndo(goldManagerObject, "Create Gold Manager");
        goldManagerObject.AddComponent<GoldManager>();

        Selection.activeGameObject = goldManagerObject;
    }

    [MenuItem("Tools/Shop/Setup Shop NPC")]
    private static void Setup()
    {
        GameObject shopNpc = GameObject.Find(ShopNpcName);

        if (shopNpc == null)
        {
            Debug.LogError($"ShopSetupTool: nenhum GameObject \"{ShopNpcName}\" encontrado na cena.");
            return;
        }

        int npcLayer = LayerMask.NameToLayer(NpcLayerName);

        if (npcLayer < 0)
        {
            Debug.LogError($"ShopSetupTool: layer \"{NpcLayerName}\" não existe no projeto.");
            return;
        }

        Undo.RecordObject(shopNpc, "Setup Shop NPC");
        shopNpc.layer = npcLayer;
        shopNpc.tag = "NPC";

        BoxCollider2D collider = shopNpc.GetComponent<BoxCollider2D>();

        if (collider == null)
            collider = Undo.AddComponent<BoxCollider2D>(shopNpc);

        collider.isTrigger = true;

        if (shopNpc.GetComponent<ShopInteractable>() == null)
            Undo.AddComponent<ShopInteractable>(shopNpc);

        BuildBagIcon(shopNpc.transform);

        EditorUtility.SetDirty(shopNpc);
        Selection.activeGameObject = shopNpc;
    }

    private static void BuildBagIcon(Transform parent)
    {
        Transform existing = parent.Find("Sale Bag Icon");

        if (existing != null)
            return;

        GameObject iconObject = new("Sale Bag Icon");
        Undo.RegisterCreatedObjectUndo(iconObject, "Create Sale Bag Icon");
        iconObject.transform.SetParent(parent, false);
        iconObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);

        SpriteRenderer renderer = iconObject.AddComponent<SpriteRenderer>();
        renderer.sprite = null; // placeholder — atribuir o sprite do saco de compras no Inspector
    }
}
