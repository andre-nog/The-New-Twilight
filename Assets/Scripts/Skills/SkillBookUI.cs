using UnityEngine;
using UnityEngine.InputSystem;

// Abre/fecha o painel do Livro de Skills — mesmo padrão de
// Assets/Scripts/Itens/InventoryScripts/InventoryManager.cs (CanvasGroup pra
// alpha/interactable/blocksRaycasts, ICancelable pra ESC também fechar).
public class SkillBookUI : MonoBehaviour, ICancelable
{
    public CanvasGroup canvasGroup;
    public InputActionReference toggleAction;

    private bool isOpen;

    private void OnEnable()
    {
        toggleAction.action.Enable();
    }

    private void OnDisable()
    {
        toggleAction.action.Disable();
    }

    private void Start()
    {
        Close();
        CancelManager.Instance.Register(this);
    }

    private void Update()
    {
        if (toggleAction.action.WasPressedThisFrame())
        {
            if (isOpen)
                Close();
            else
                Open();
        }
    }

    public void Open()
    {
        isOpen = true;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void Close()
    {
        isOpen = false;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public bool CanCancel()
    {
        return isOpen;
    }

    public void Cancel()
    {
        Close();
    }

    public int Priority => 100;

    private void OnDestroy()
    {
        if (CancelManager.Instance != null)
            CancelManager.Instance.Unregister(this);
    }
}
