using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Quest", menuName = "Quests/Quest")]
public class QuestSO : ScriptableObject
{
    // Id estável usado como chave no save (QuestSave.questId) — autoral (o designer
    // escreve à mão), mas ganha um GUID automático de rede de segurança se for
    // deixado vazio, no mesmo padrão não-destrutivo de ItemSO/Skill/ClassDefinitionSO
    // (só preenche se estiver vazio; nunca sobrescreve um id já digitado).
    public string id;
    public string questName;

    [TextArea]
    public string description;

    public QuestObjectiveType objectiveType = QuestObjectiveType.KillEnemies;

    [Tooltip("Referência direta ao EnemyArchetypeSO alvo — antes era um texto que precisava bater com EnemyStats.DisplayName (frágil a rename/typo); agora é comparado por referência de asset, então nunca dessincroniza.")]
    public EnemyArchetypeSO targetArchetype;

    public int requiredAmount = 10;
    public int xpReward = 200;

    [Tooltip("Prefixo mostrado na janela e no tracker, ex.: \"Kill Goblins\".")]
    public string objectiveLabel = "Kill Enemies";

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!string.IsNullOrEmpty(id))
            return;

        string path = AssetDatabase.GetAssetPath(this);

        if (string.IsNullOrEmpty(path))
            return;

        id = AssetDatabase.AssetPathToGUID(path);
        EditorUtility.SetDirty(this);
    }
#endif
}
