using UnityEngine;

[RequireComponent(typeof(NPCInteractable))]
public class QuestGiverIndicator : MonoBehaviour
{
    [SerializeField] private GameObject availableIcon;
    [SerializeField] private GameObject readyIcon;

    private NPCInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<NPCInteractable>();
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
        // Pull em vez de depender do timing do evento de seed do QuestManager —
        // garante o estado certo mesmo se este objeto ativar depois do seed.
        Refresh();
    }

    // A quest que mudou pode não ser mais a CurrentQuest no instante do evento
    // (ex.: entregar a quest N dispara OnQuestUpdated(questN), mas é a N+1 que
    // vira CurrentQuest) — por isso o filtro é "pertence a esta NPC", não
    // "é a atual", e o Refresh() sempre lê CurrentQuest de novo, já atualizada.
    private void HandleQuestUpdated(QuestSO changed)
    {
        if (interactable.OwnsQuest(changed))
            Refresh();
    }

    private void Refresh()
    {
        QuestSO current = interactable.CurrentQuest;

        if (current == null || QuestManager.Instance == null)
        {
            availableIcon.SetActive(false);
            readyIcon.SetActive(false);
            return;
        }

        QuestState state = QuestManager.Instance.GetQuestState(current);

        availableIcon.SetActive(state == QuestState.Available);
        readyIcon.SetActive(state == QuestState.ReadyToComplete);
    }
}
