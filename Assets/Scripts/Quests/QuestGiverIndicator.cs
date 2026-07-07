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

    private void HandleQuestUpdated(QuestSO changed)
    {
        if (changed == interactable.Quest)
            Refresh();
    }

    private void Refresh()
    {
        if (interactable.Quest == null || QuestManager.Instance == null)
        {
            availableIcon.SetActive(false);
            readyIcon.SetActive(false);
            return;
        }

        QuestState state = QuestManager.Instance.GetQuestState(interactable.Quest);

        availableIcon.SetActive(state == QuestState.Available);
        readyIcon.SetActive(state == QuestState.ReadyToComplete);
    }
}
