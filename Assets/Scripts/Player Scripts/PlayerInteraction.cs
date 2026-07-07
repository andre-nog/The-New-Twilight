using UnityEngine;
using UnityEngine.InputSystem;

// Espelha Player_Combat.MoveToTargetAndAttack: sem NavMeshAgent, sem callback de
// chegada — só um Update() fazendo Vector2.Distance contra a distância de
// interação a cada frame, chamando playerMovement.MoveTo() até entrar no alcance.
public class PlayerInteraction : MonoBehaviour
{
    public PlayerMovement playerMovement;
    public Player_Combat playerCombat;

    public InputActionReference interactClick;

    [SerializeField] private LayerMask npcLayerMask;
    [SerializeField] private float interactionDistance = 1.75f;

    [Tooltip("Cursor customizado pro hover de NPC — se vazio, usa um círculo dourado gerado em runtime como placeholder.")]
    [SerializeField] private Texture2D customCursor;

    [SerializeField] private Vector2 cursorHotspot = new(10f, 10f);

    private NPCInteractable pendingTarget;
    private bool movingToInteract;
    private NPCInteractable hoveredTarget;
    private SpriteRenderer hoveredRenderer;
    private Color hoveredOriginalColor;

    private static Texture2D generatedCursor;
    private static readonly Color CursorColor = new(1f, 0.86f, 0.35f, 1f);

    // Sprite não fica mais "maior" no hover (o Outline escalado parecia glitchado)
    // — em vez disso clareia a cor multiplicando acima de 1, que estoura os canais
    // pro branco sem mudar o tamanho do sprite.
    private const float BrightnessMultiplier = 1.6f;

    private void OnEnable()
    {
        interactClick.action.Enable();
    }

    private void OnDisable()
    {
        interactClick.action.Disable();
        ClearHover();
    }

    private void Update()
    {
        UpdateHover();

        if (interactClick.action.WasPressedThisFrame())
            CheckNpcClick();

        if (movingToInteract)
        {
            // Movimento manual durante o walk-to-NPC cancela, igual PlayerMovement
            // já faz com movingToAttack quando o jogador anda com WASD.
            if (playerMovement.move.action.ReadValue<Vector2>() != Vector2.zero)
            {
                CancelInteract();
                return;
            }

            MoveToTargetAndInteract();
        }
    }

    private void UpdateHover()
    {
        NPCInteractable target = FindNpcUnderMouse();

        if (target == hoveredTarget)
            return;

        if (hoveredRenderer != null)
            hoveredRenderer.color = hoveredOriginalColor;

        hoveredTarget = target;
        hoveredRenderer = null;

        if (hoveredTarget != null)
        {
            hoveredRenderer = hoveredTarget.GetComponent<SpriteRenderer>();

            if (hoveredRenderer != null)
            {
                hoveredOriginalColor = hoveredRenderer.color;
                Color bright = hoveredOriginalColor * BrightnessMultiplier;
                bright.a = hoveredOriginalColor.a;
                hoveredRenderer.color = bright;
            }

            Cursor.SetCursor(GetCursorTexture(), cursorHotspot, CursorMode.Auto);
            HoverCursorState.CurrentOwner = this;
        }
        else if (HoverCursorState.CurrentOwner == (object)this)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            HoverCursorState.CurrentOwner = null;
        }
    }

    private void ClearHover()
    {
        if (hoveredRenderer != null)
            hoveredRenderer.color = hoveredOriginalColor;

        hoveredTarget = null;
        hoveredRenderer = null;

        if (HoverCursorState.CurrentOwner == (object)this)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            HoverCursorState.CurrentOwner = null;
        }
    }

    private void CheckNpcClick()
    {
        NPCInteractable interactable = FindNpcUnderMouse();

        if (interactable != null)
            BeginInteract(interactable);
    }

    // Mesmo formato de PlayerTargeting.CheckEnemyClick (OverlapPointAll + maior
    // sortingOrder), só que filtrando pela layer/tag "NPC" em vez de "Enemy" —
    // reaproveitado tanto pelo clique quanto pelo hover a cada frame.
    private NPCInteractable FindNpcUnderMouse()
    {
        if (Mouse.current == null || Camera.main == null)
            return null;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        Collider2D[] hits = Physics2D.OverlapPointAll(mousePos, npcLayerMask);

        if (hits.Length == 0)
            return null;

        GameObject bestTarget = null;
        int bestSortingOrder = int.MinValue;

        foreach (Collider2D hit in hits)
        {
            GameObject hitObject = hit.attachedRigidbody != null
                ? hit.attachedRigidbody.gameObject
                : hit.gameObject;

            if (!hitObject.CompareTag("NPC"))
                continue;

            SpriteRenderer sr = hitObject.GetComponent<SpriteRenderer>();
            int sortingOrder = sr != null ? sr.sortingOrder : int.MinValue;

            if (sortingOrder > bestSortingOrder)
            {
                bestSortingOrder = sortingOrder;
                bestTarget = hitObject;
            }
        }

        return bestTarget != null ? bestTarget.GetComponent<NPCInteractable>() : null;
    }

    public void BeginInteract(NPCInteractable target)
    {
        playerCombat.CancelMoveToAttack();

        pendingTarget = target;

        float distance = Vector2.Distance(transform.position, target.transform.position);

        if (distance <= interactionDistance)
        {
            Arrive();
            return;
        }

        movingToInteract = true;
        playerMovement.autoMoving = true;
    }

    public void CancelInteract()
    {
        movingToInteract = false;
        pendingTarget = null;
        playerMovement.CancelAutoMove();
    }

    private void MoveToTargetAndInteract()
    {
        if (pendingTarget == null)
        {
            CancelInteract();
            return;
        }

        float distance = Vector2.Distance(transform.position, pendingTarget.transform.position);

        if (distance <= interactionDistance)
        {
            Arrive();
            return;
        }

        playerMovement.MoveTo(pendingTarget.transform.position);
    }

    private void Arrive()
    {
        NPCInteractable target = pendingTarget;

        movingToInteract = false;
        pendingTarget = null;
        playerMovement.CancelAutoMove();

        target.OnPlayerArrived();
    }

    private Texture2D GetCursorTexture()
    {
        if (customCursor != null)
            return customCursor;

        if (generatedCursor == null)
            generatedCursor = CursorTextureFactory.CreateOrb(CursorColor);

        return generatedCursor;
    }
}
