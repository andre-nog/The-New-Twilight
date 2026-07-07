using UnityEngine;

[CreateAssetMenu(fileName = "New Quest", menuName = "Quests/Quest")]
public class QuestSO : ScriptableObject
{
    public string id;
    public string questName;

    [TextArea]
    public string description;

    public QuestObjectiveType objectiveType = QuestObjectiveType.KillEnemies;

    [Tooltip("Deve bater exatamente com EnemyStats.DisplayName (vindo do EnemyArchetypeSO do inimigo alvo).")]
    public string targetId;

    public int requiredAmount = 10;
    public int xpReward = 200;

    [Tooltip("Prefixo mostrado na janela e no tracker, ex.: \"Kill Goblins\".")]
    public string objectiveLabel = "Kill Enemies";
}
