using TMPro;
using UnityEditor;
using UnityEngine;

// NPCInteractable já vem cravado no QuestGiver via cena (aponta pro asset
// KillGoblins). Essa ferramenta cuida da parte arriscada de serializar à mão:
// os dois filhos de TextMeshPro 3D (mundo, não UI) acima da cabeça e o
// QuestGiverIndicator que liga tudo — TextMeshPro 3D tem estado interno de
// malha/renderer que só fica correto quando criado via AddComponent em vez de
// YAML escrito a mão. O contorno de hover (HoverOutline) não precisa de setup
// aqui — é adicionado dinamicamente em runtime por PlayerInteraction.
public static class QuestGiverSetupTool
{
    private const string QuestGiverName = "QuestGiver";
    private static readonly Color GoldAccent = new(1f, 0.86f, 0.35f, 1f);

    [MenuItem("Tools/Quests/Setup Quest Giver Indicator")]
    private static void Setup()
    {
        GameObject questGiver = GameObject.Find(QuestGiverName);

        if (questGiver == null)
        {
            Debug.LogError($"QuestGiverSetupTool: nenhum GameObject \"{QuestGiverName}\" encontrado na cena.");
            return;
        }

        GameObject availableIcon = BuildIcon(questGiver.transform, "Available Icon", "!");
        GameObject readyIcon = BuildIcon(questGiver.transform, "Ready Icon", "?");
        readyIcon.SetActive(false);

        // Versão antiga do tool criava um filho "Highlight" (silhueta escalada) —
        // substituído pelo HoverOutline dinâmico; remove resíduo se existir.
        Transform staleHighlight = questGiver.transform.Find("Highlight");

        if (staleHighlight != null)
            Undo.DestroyObjectImmediate(staleHighlight.gameObject);

        QuestGiverIndicator indicator = questGiver.GetComponent<QuestGiverIndicator>();

        if (indicator == null)
            indicator = Undo.AddComponent<QuestGiverIndicator>(questGiver);

        SerializedObject serializedIndicator = new(indicator);
        serializedIndicator.FindProperty("availableIcon").objectReferenceValue = availableIcon;
        serializedIndicator.FindProperty("readyIcon").objectReferenceValue = readyIcon;
        serializedIndicator.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(questGiver);
        Selection.activeGameObject = questGiver;
    }

    private static GameObject BuildIcon(Transform parent, string objectName, string glyph)
    {
        Transform existing = parent.Find(objectName);

        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject iconObject = new(objectName);
        Undo.RegisterCreatedObjectUndo(iconObject, $"Create {objectName}");
        iconObject.transform.SetParent(parent, false);
        iconObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);

        TextMeshPro text = iconObject.AddComponent<TextMeshPro>();
        text.text = glyph;
        text.fontSize = 7f;
        text.color = GoldAccent;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        // Fica na fonte padrão (TMP default) por pedido — Bangers SDF não
        // encaixava bem no glifo "!"/"?" nesse tamanho.

        return iconObject;
    }
}
