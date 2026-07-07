using System;
using System.Collections.Generic;
using UnityEngine;

// Fonte única de verdade pro estado de quests em runtime. Segue o mesmo
// convencionamento de singleton usado por InventoryManager/EquipmentManager
// (Instance simples, sem DontDestroyOnLoad — só GameManager persiste entre cenas).
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    // Estático pra UI (indicador da NPC, tracker) poder se inscrever em OnEnable
    // sem depender da ordem de inicialização do singleton.
    public static event Action<QuestSO> OnQuestUpdated;

    [SerializeField] private QuestSO[] allQuests;
    public IReadOnlyList<QuestSO> AllQuests => allQuests;

    private class QuestRuntime
    {
        public QuestState state;
        public int progress;
    }

    private readonly Dictionary<string, QuestRuntime> runtime = new();
    private ExpManager expManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        expManager = FindAnyObjectByType<ExpManager>();

        foreach (QuestSO quest in allQuests)
        {
            if (quest == null || runtime.ContainsKey(quest.id))
                continue;

            runtime[quest.id] = new QuestRuntime { state = QuestState.Available, progress = 0 };
        }
    }

    private void OnEnable()
    {
        Enemy_Health.OnMonsterDefeated += HandleMonsterDefeated;
    }

    private void OnDisable()
    {
        Enemy_Health.OnMonsterDefeated -= HandleMonsterDefeated;
    }

    private QuestRuntime GetRuntime(QuestSO quest)
    {
        if (quest == null)
            return null;

        if (!runtime.TryGetValue(quest.id, out QuestRuntime state))
        {
            state = new QuestRuntime { state = QuestState.Available, progress = 0 };
            runtime[quest.id] = state;
        }

        return state;
    }

    public QuestState GetQuestState(QuestSO quest)
    {
        QuestRuntime state = GetRuntime(quest);
        return state != null ? state.state : QuestState.NotStarted;
    }

    public int GetProgress(QuestSO quest)
    {
        QuestRuntime state = GetRuntime(quest);
        return state != null ? state.progress : 0;
    }

    public bool CanAccept(QuestSO quest)
    {
        return GetQuestState(quest) == QuestState.Available;
    }

    public bool CanComplete(QuestSO quest)
    {
        return GetQuestState(quest) == QuestState.ReadyToComplete;
    }

    public void AcceptQuest(QuestSO quest)
    {
        QuestRuntime state = GetRuntime(quest);

        if (state == null || state.state != QuestState.Available)
            return;

        state.state = QuestState.InProgress;
        state.progress = 0;

        OnQuestUpdated?.Invoke(quest);
    }

    public void CompleteQuest(QuestSO quest)
    {
        QuestRuntime state = GetRuntime(quest);

        if (state == null || state.state != QuestState.ReadyToComplete)
            return;

        state.state = QuestState.TurnedIn;

        expManager?.GainExperience(quest.xpReward);

        OnQuestUpdated?.Invoke(quest);
    }

    private void HandleMonsterDefeated(int exp, string displayName)
    {
        foreach (QuestSO quest in allQuests)
        {
            if (quest == null || quest.objectiveType != QuestObjectiveType.KillEnemies)
                continue;

            if (quest.targetId != displayName)
                continue;

            QuestRuntime state = GetRuntime(quest);

            if (state.state != QuestState.InProgress)
                continue;

            state.progress = Mathf.Min(state.progress + 1, quest.requiredAmount);

            if (state.progress >= quest.requiredAmount)
                state.state = QuestState.ReadyToComplete;

            OnQuestUpdated?.Invoke(quest);
        }
    }

    private QuestSO FindQuestById(string id)
    {
        foreach (QuestSO quest in allQuests)
        {
            if (quest != null && quest.id == id)
                return quest;
        }

        return null;
    }

    public List<QuestSave> GetState()
    {
        List<QuestSave> result = new();

        foreach (QuestSO quest in allQuests)
        {
            if (quest == null)
                continue;

            QuestRuntime state = GetRuntime(quest);

            result.Add(new QuestSave
            {
                questId = quest.id,
                state = (int)state.state,
                progress = state.progress
            });
        }

        return result;
    }

    // Reconstrói do zero a partir do save — todo quest não presente na lista
    // volta pro estado seed (Available/0), igual InventoryManager.ApplyState
    // limpa slots não presentes.
    public void ApplyState(List<QuestSave> state)
    {
        runtime.Clear();

        foreach (QuestSO quest in allQuests)
        {
            if (quest != null)
                runtime[quest.id] = new QuestRuntime { state = QuestState.Available, progress = 0 };
        }

        if (state != null)
        {
            foreach (QuestSave saved in state)
            {
                if (runtime.TryGetValue(saved.questId, out QuestRuntime questState))
                {
                    questState.state = (QuestState)saved.state;
                    questState.progress = saved.progress;
                }
            }
        }

        foreach (QuestSO quest in allQuests)
        {
            if (quest != null)
                OnQuestUpdated?.Invoke(quest);
        }
    }
}
