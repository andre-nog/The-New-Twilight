using TMPro;
using UnityEngine;

// Uma única janela com 3 modos em vez de 3 classes separadas — os 3 estados
// (Accept/InProgress/Complete) compartilham quase todo o layout, só o rodapé
// de botões muda.
public class QuestWindow : MonoBehaviour, ICancelable
{
    public static QuestWindow Instance { get; private set; }

    public enum Mode
    {
        Accept,
        InProgress,
        Complete,
        Dialogue
    }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private GameObject acceptButton;
    [SerializeField] private GameObject declineButton;
    [SerializeField] private GameObject confirmButton;
    [SerializeField] private GameObject cancelButton;

    private QuestSO currentQuest;
    private bool isOpen;
    private Vector3 openPlayerPosition;

    // Mesmo epsilon usado em PlayerMovement.FollowPath pra considerar "chegou"
    // num corner — reaproveitado aqui como "o jogador se moveu o suficiente pra
    // fechar a janela".
    private const float MoveCloseThreshold = 0.1f;

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
        CancelManager.Instance.Register(this);
        Close();
    }

    private void OnDestroy()
    {
        if (CancelManager.Instance != null)
            CancelManager.Instance.Unregister(this);

        if (Instance == this)
            Instance = null;
    }

    public void Configure(
        CanvasGroup group,
        TMP_Text title,
        TMP_Text description,
        TMP_Text objective,
        TMP_Text reward,
        GameObject accept,
        GameObject decline,
        GameObject confirm,
        GameObject cancel)
    {
        canvasGroup = group;
        titleText = title;
        descriptionText = description;
        objectiveText = objective;
        rewardText = reward;
        acceptButton = accept;
        declineButton = decline;
        confirmButton = confirm;
        cancelButton = cancel;
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (GameManager.Instance == null || GameManager.Instance.Player == null)
            return;

        float moved = Vector2.Distance(GameManager.Instance.Player.position, openPlayerPosition);

        if (moved > MoveCloseThreshold)
            Close();
    }

    public void OpenAccept(QuestSO quest)
    {
        Open(quest, Mode.Accept);
    }

    public void OpenInProgress(QuestSO quest)
    {
        Open(quest, Mode.InProgress);
    }

    public void OpenComplete(QuestSO quest)
    {
        Open(quest, Mode.Complete);
    }

    // Sem QuestSO — usado quando a NPC não tem quest disponível/pronta pra
    // entregar (cadeia inteira entregue/TurnedIn, ou sem quest atribuída).
    public void OpenDialogue(string speakerName, string line)
    {
        currentQuest = null;

        titleText.text = speakerName;
        descriptionText.text = line;
        objectiveText.text = string.Empty;
        rewardText.text = string.Empty;

        acceptButton.SetActive(false);
        declineButton.SetActive(false);
        confirmButton.SetActive(false);
        cancelButton.SetActive(true);

        ShowWindow();
    }

    private void Open(QuestSO quest, Mode mode)
    {
        if (quest == null || QuestManager.Instance == null)
            return;

        currentQuest = quest;

        titleText.text = quest.questName;
        descriptionText.text = quest.description;
        objectiveText.text = $"{quest.objectiveLabel}: {QuestManager.Instance.GetProgress(quest)}/{quest.requiredAmount}";
        rewardText.text = $"Reward: {quest.xpReward} XP";

        acceptButton.SetActive(mode == Mode.Accept);
        // Botão de rodapé, só no modo Accept — decisão explícita "Accept ou
        // Cancel" lado a lado, além do "X" genérico do canto (cancelButton),
        // que continua cobrindo "só fechar" em qualquer modo.
        declineButton.SetActive(mode == Mode.Accept);
        confirmButton.SetActive(mode == Mode.Complete);
        cancelButton.SetActive(true);

        ShowWindow();
    }

    private void ShowWindow()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        isOpen = true;

        if (GameManager.Instance != null && GameManager.Instance.Player != null)
            openPlayerPosition = GameManager.Instance.Player.position;
    }

    public void Close()
    {
        isOpen = false;
        currentQuest = null;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnAcceptClicked()
    {
        if (currentQuest == null || QuestManager.Instance == null)
            return;

        QuestManager.Instance.AcceptQuest(currentQuest);
        Close();
    }

    public void OnConfirmClicked()
    {
        if (currentQuest == null || QuestManager.Instance == null)
            return;

        QuestManager.Instance.CompleteQuest(currentQuest);
        Close();
    }

    public void OnCancelClicked()
    {
        Close();
    }

    public bool CanCancel()
    {
        return isOpen;
    }

    public void Cancel()
    {
        Close();
    }

    public int Priority => 150;
}
