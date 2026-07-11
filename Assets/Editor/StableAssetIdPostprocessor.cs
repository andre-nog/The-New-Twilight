using UnityEditor;
using UnityEngine;

// Preenche o id estável (ver IStableAssetId) de qualquer ScriptableObject
// importado/criado/duplicado — sem isso, um asset (skill, item...) que nunca foi
// aberto no Inspector fica com id vazio pra sempre, e some silenciosamente na
// primeira vez que passar por save/load (ver ItemSO/Skill.EnsureId).
public class StableAssetIdPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets)
        {
            if (!path.EndsWith(".asset"))
                continue;

            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) is IStableAssetId asset)
                asset.EnsureId();
        }
    }
}
