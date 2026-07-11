using System;
using System.Collections.Generic;
using UnityEngine;

// DTO de save deste manager — ver ISaveParticipant/SaveSystem.
[Serializable]
public class QuestsSave
{
    public List<QuestSave> quests = new();
}

// Fonte única de verdade pro estado de quests em runtime. Segue o mesmo
// convencionamento de singleton usado por InventoryManager/EquipmentManager
// (Instance simples, sem DontDestroyOnLoad — só GameManager persiste entre cenas).
public class QuestManager : MonoBehaviour, ISaveParticipant
{
    public string SaveKey => "quests";
    public int SchemaVersion => 1;
    public int Order => 0;

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

    // Entradas cujo questId não bate com nenhum QuestSO em allQuests (asset
    // renomeado/removido durante balanceamento) — nunca descartadas silenciosamente,
    // ficam aqui até o id voltar a existir (ver GetState).
    private readonly List<QuestSave> unresolvedQuests = new();

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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SaveRegistry.Unregister(this);
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

        if (expManager != null)
            expManager.GainExperience(quest.xpReward);

        if (GoldManager.Instance != null)
            GoldManager.Instance.AddGold(quest.goldReward);

        OnQuestUpdated?.Invoke(quest);
    }

    private void HandleMonsterDefeated(int exp, int gold, EnemyArchetypeSO archetype, Vector3 position)
    {
        foreach (QuestSO quest in allQuests)
        {
            if (quest == null || quest.objectiveType != QuestObjectiveType.KillEnemies)
                continue;

            if (!TargetsArchetype(quest, archetype))
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

    // Mais de um archetype pode contar pro mesmo objetivo (ex.: variante melee
    // e ranged do "mesmo" inimigo, como Goblin) — ver QuestSO.targetArchetypes.
    private static bool TargetsArchetype(QuestSO quest, EnemyArchetypeSO archetype)
    {
        if (quest.targetArchetypes == null)
            return false;

        foreach (EnemyArchetypeSO target in quest.targetArchetypes)
        {
            if (target == archetype)
                return true;
        }

        return false;
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

    // Novo Jogo = todo quest volta pro estado seed (Available/0). Chamado só pelo
    // SaveSystem — equivalente a RestoreState com uma lista vazia.
    public void InitializeNewGame()
    {
        ResetToSeed();
    }

    private void ResetToSeed()
    {
        runtime.Clear();
        unresolvedQuests.Clear();

        foreach (QuestSO quest in allQuests)
        {
            if (quest != null)
                runtime[quest.id] = new QuestRuntime { state = QuestState.Available, progress = 0 };
        }

        foreach (QuestSO quest in allQuests)
        {
            if (quest != null)
                OnQuestUpdated?.Invoke(quest);
        }
    }

    public string CaptureState()
    {
        List<QuestSave> quests = new();

        foreach (QuestSO quest in allQuests)
        {
            if (quest == null)
                continue;

            QuestRuntime state = GetRuntime(quest);

            quests.Add(new QuestSave
            {
                questId = quest.id,
                state = state.state.ToString(),
                progress = state.progress
            });
        }

        quests.AddRange(unresolvedQuests);

        return JsonUtility.ToJson(new QuestsSave { quests = quests });
    }

    // Reconstrói do zero a partir do save — todo quest não presente na lista
    // volta pro estado seed (Available/0), igual InventoryManager.RestoreState
    // limpa slots não presentes.
    public void RestoreState(string json, int schemaVersion)
    {
        ResetToSeed();

        QuestsSave save = JsonUtility.FromJson<QuestsSave>(json);

        if (save?.quests == null)
            return;

        foreach (QuestSave saved in save.quests)
        {
            if (runtime.TryGetValue(saved.questId, out QuestRuntime questState))
            {
                // Estado inválido/desconhecido (save corrompido ou editado à mão)
                // fica no seed Available em vez de corromper o runtime — nunca crasha.
                if (Enum.TryParse(saved.state, out QuestState parsedState))
                    questState.state = parsedState;
                else
                    Debug.LogWarning($"QuestManager: estado '{saved.state}' inválido pra quest '{saved.questId}' — mantendo Available.");

                questState.progress = saved.progress;
            }
            else
            {
                Debug.LogWarning($"QuestManager: questId '{saved.questId}' não encontrado em allQuests — preservado cru em vez de descartado.");
                unresolvedQuests.Add(saved);
            }
        }

        foreach (QuestSO quest in allQuests)
        {
            if (quest != null)
                OnQuestUpdated?.Invoke(quest);
        }
    }
}
