using UnityEngine;

// Uma NPC pode ter uma cadeia de quests, oferecidas uma de cada vez, em ordem —
// CurrentQuest sempre aponta pra primeira ainda não entregue (TurnedIn) da
// lista; quando todas estiverem entregues, cai no diálogo idle, igual a uma NPC
// sem quest nenhuma. O gate da cadeia é só isso: QuestManager já trata toda
// quest como Available desde o início (não existe um estado "bloqueada"), mas
// esta NPC nunca oferece a quest N+1 antes de N chegar em TurnedIn.
public class NPCInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string npcName = "Villager";
    [SerializeField] private QuestSO[] quests;

    // Sem cursor próprio — usa o fallback padrão (orb dourado) do PlayerInteraction.
    public Texture2D CursorTexture => null;

    [Tooltip("Falas mostradas quando não há quest disponível/pronta (cadeia inteira entregue, ou sem quest atribuída). Uma é sorteada por interação.")]
    [SerializeField] private string[] idleDialogue =
    {
        "Nice weather we're having.",
        "Watch yourself out there, goblins have been causing trouble.",
        "I've got nothing for you right now. Come back later."
    };

    // Primeira quest da cadeia ainda não entregue — null se a lista estiver
    // vazia ou todas já tiverem sido entregues.
    public QuestSO CurrentQuest => GetCurrentQuest();

    // Usado por QuestGiverIndicator pra saber se um QuestSO que acabou de
    // mudar de estado pertence a esta NPC (mesmo que não seja mais a
    // CurrentQuest no momento do evento — ver QuestGiverIndicator.HandleQuestUpdated).
    public bool OwnsQuest(QuestSO quest)
    {
        if (quest == null || quests == null)
            return false;

        foreach (QuestSO candidate in quests)
        {
            if (candidate == quest)
                return true;
        }

        return false;
    }

    private QuestSO GetCurrentQuest()
    {
        if (quests == null || QuestManager.Instance == null)
            return null;

        foreach (QuestSO quest in quests)
        {
            if (quest != null && QuestManager.Instance.GetQuestState(quest) != QuestState.TurnedIn)
                return quest;
        }

        return null;
    }

    public void OnPlayerArrived()
    {
        if (QuestWindow.Instance == null)
            return;

        QuestSO quest = CurrentQuest;

        QuestState state = quest != null && QuestManager.Instance != null
            ? QuestManager.Instance.GetQuestState(quest)
            : QuestState.NotStarted;

        switch (state)
        {
            case QuestState.Available:
                QuestWindow.Instance.OpenAccept(quest);
                break;

            case QuestState.InProgress:
                QuestWindow.Instance.OpenInProgress(quest);
                break;

            case QuestState.ReadyToComplete:
                QuestWindow.Instance.OpenComplete(quest);
                break;

            default:
                ShowIdleDialogue();
                break;
        }
    }

    private void ShowIdleDialogue()
    {
        if (idleDialogue == null || idleDialogue.Length == 0)
            return;

        string line = idleDialogue[Random.Range(0, idleDialogue.Length)];
        QuestWindow.Instance.OpenDialogue(npcName, line);
    }
}
