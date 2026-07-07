using UnityEngine;

// Pool fixo de linhas (mesma convenção dos 9 slots fixos do SkillBarUI) em vez
// de Instantiate em runtime — lê QuestManager.AllQuests direto, então uma
// quest nova só precisa ser adicionada no array do QuestManager, sem
// nenhuma mudança aqui.
public class QuestTrackerHUD : MonoBehaviour
{
    [SerializeField] private QuestTrackerRowUI[] rows;

    public void Configure(QuestTrackerRowUI[] rowInstances)
    {
        rows = rowInstances;
    }

    private void OnEnable()
    {
        QuestManager.OnQuestUpdated += HandleQuestUpdated;
    }

    private void OnDisable()
    {
        QuestManager.OnQuestUpdated -= HandleQuestUpdated;
    }

    private void Start()
    {
        RefreshAll();
    }

    private void HandleQuestUpdated(QuestSO quest)
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (rows == null || QuestManager.Instance == null)
            return;

        int rowIndex = 0;

        foreach (QuestSO quest in QuestManager.Instance.AllQuests)
        {
            if (quest == null)
                continue;

            QuestState state = QuestManager.Instance.GetQuestState(quest);

            if (state != QuestState.InProgress && state != QuestState.ReadyToComplete)
                continue;

            if (rowIndex >= rows.Length)
                break;

            string objective = $"{quest.objectiveLabel} ({QuestManager.Instance.GetProgress(quest)}/{quest.requiredAmount})";
            rows[rowIndex].Setup(quest.questName, objective);
            rows[rowIndex].gameObject.SetActive(true);
            rowIndex++;
        }

        for (int i = rowIndex; i < rows.Length; i++)
            rows[i].gameObject.SetActive(false);
    }
}
