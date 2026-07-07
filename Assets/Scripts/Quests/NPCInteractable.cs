using UnityEngine;

// Um único quest por NPC em v1 — um giver com múltiplos quests exigiria um
// QuestSO[] + lógica de escolha, fora do escopo atual.
public class NPCInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string npcName = "Villager";
    [SerializeField] private QuestSO quest;

    // Sem cursor próprio — usa o fallback padrão (orb dourado) do PlayerInteraction.
    public Texture2D CursorTexture => null;

    [Tooltip("Falas mostradas quando não há quest disponível/pronta (NotStarted, TurnedIn, ou sem quest atribuída). Uma é sorteada por interação.")]
    [SerializeField] private string[] idleDialogue =
    {
        "Nice weather we're having.",
        "Watch yourself out there, goblins have been causing trouble.",
        "I've got nothing for you right now. Come back later."
    };

    public QuestSO Quest => quest;

    public void OnPlayerArrived()
    {
        if (QuestWindow.Instance == null)
            return;

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
