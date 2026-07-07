using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerTargeting : MonoBehaviour, ICancelable
{
    public InputActionReference selectTarget;
    public InputActionReference targetNext;

    [SerializeField] private LayerMask enemyLayerMask;

    [Tooltip("Cursor customizado pro hover de inimigo — se vazio, usa um círculo vermelho gerado em runtime como placeholder.")]
    [SerializeField] private Texture2D customCursor;

    [SerializeField] private Vector2 cursorHotspot = new(10f, 10f);

    public GameObject currentTarget;

    private GameObject hoveredEnemy;
    private static readonly Color HoverOutlineColor = Color.red;
    private static readonly Color SelectionColor = Color.red;
    private static Texture2D generatedCursor;

    private void OnEnable()
    {
        selectTarget.action.Enable();
        targetNext.action.Enable();
    }

    private void OnDisable()
    {
        selectTarget.action.Disable();
        targetNext.action.Disable();

        ClearHover();
    }

    private void Start()
    {
        CancelManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        if (CancelManager.Instance != null)
            CancelManager.Instance.Unregister(this);
    }

    private void Update()
    {
        UpdateHover();

        if (selectTarget.action.WasPressedThisFrame())
            CheckEnemyClick();

        if (targetNext.action.WasPressedThisFrame())
            SelectNextEnemy();
    }

    private void UpdateHover()
    {
        GameObject target = FindEnemyUnderMouse();

        if (target == hoveredEnemy)
            return;

        if (hoveredEnemy != null)
            hoveredEnemy.GetComponent<HoverOutline>()?.SetVisible(false, HoverOutlineColor);

        hoveredEnemy = target;

        if (hoveredEnemy != null)
        {
            HoverOutline outline = hoveredEnemy.GetComponent<HoverOutline>();

            if (outline == null)
                outline = hoveredEnemy.AddComponent<HoverOutline>();

            outline.SetVisible(true, HoverOutlineColor);

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
        if (hoveredEnemy != null)
        {
            hoveredEnemy.GetComponent<HoverOutline>()?.SetVisible(false, HoverOutlineColor);
            hoveredEnemy = null;
        }

        if (HoverCursorState.CurrentOwner == (object)this)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            HoverCursorState.CurrentOwner = null;
        }
    }

    private Texture2D GetCursorTexture()
    {
        if (customCursor != null)
            return customCursor;

        if (generatedCursor == null)
            generatedCursor = CursorTextureFactory.CreateOrb(Color.red);

        return generatedCursor;
    }

    private void CheckEnemyClick()
    {
        GameObject bestTarget = FindEnemyUnderMouse();

        if (bestTarget != null)
            SelectTarget(bestTarget);
    }

    // Reaproveitado tanto pelo clique (CheckEnemyClick) quanto pelo hover a cada
    // frame (UpdateHover) — mesmo OverlapPointAll + maior sortingOrder de sempre.
    private GameObject FindEnemyUnderMouse()
    {
        if (Mouse.current == null || Camera.main == null)
            return null;

        // Mesma correção de PlayerInteraction.FindNpcUnderMouse — sem isso, um
        // clique/hover em cima de uma janela de UI ainda acertava o inimigo no
        // mundo por baixo dela.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return null;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(
            Mouse.current.position.ReadValue());

        Collider2D[] hits = Physics2D.OverlapPointAll(mousePos, enemyLayerMask);

        if (hits.Length == 0)
            return null;

        GameObject bestTarget = null;
        int bestSortingOrder = int.MinValue;

        foreach (Collider2D hit in hits)
        {
            GameObject hitObject = hit.attachedRigidbody != null
                ? hit.attachedRigidbody.gameObject
                : hit.gameObject;

            if (!hitObject.CompareTag("Enemy"))
                continue;

            SpriteRenderer sr = hitObject.GetComponent<SpriteRenderer>();
            int sortingOrder = sr != null ? sr.sortingOrder : int.MinValue;

            if (sortingOrder > bestSortingOrder)
            {
                bestSortingOrder = sortingOrder;
                bestTarget = hitObject;
            }
        }

        return bestTarget;
    }

    private void SelectTarget(GameObject target)
    {
        if (target == currentTarget)
            return;

        ClearTarget();

        currentTarget = target;

        SelectionCircle circle = currentTarget.GetComponent<SelectionCircle>();

        if (circle == null)
            circle = currentTarget.AddComponent<SelectionCircle>();

        circle.SetVisible(true, SelectionColor);
    }

    private void SelectNextEnemy()
    {
        // Enemy_Health.Active substitui FindGameObjectsWithTag — mesma lista de
        // inimigos vivos, sem varrer a cena inteira a cada Tab.
        if (Enemy_Health.Active.Count == 0)
            return;

        List<GameObject> visibleEnemies = new();

        foreach (Enemy_Health enemyHealth in Enemy_Health.Active)
        {
            GameObject enemy = enemyHealth.gameObject;
            // ALTERADO - trocado SpriteRenderer.bounds por Collider2D.bounds.
            // Isso alinha a checagem de visibilidade com a mesma área usada pra
            // clicar no inimigo (CheckEnemyClick usa Physics2D.OverlapPointAll no
            // collider). Sprite bounds pode incluir bastante espaço transparente
            // nas bordas, então o collider costuma ser mais fiel à área "real"
            // do inimigo e evita selecionar algo que só tem pixel transparente na tela.
            Collider2D col = enemy.GetComponent<Collider2D>();

            bool isVisible = col != null
                ? IsBoundsVisible(col.bounds, Camera.main)
                : IsPointVisible(enemy.transform.position); // fallback caso não tenha Collider2D

            if (isVisible)
                visibleEnemies.Add(enemy);
        }

        if (visibleEnemies.Count == 0)
            return;

        visibleEnemies.Sort((a, b) =>
            Vector2.Distance(transform.position, a.transform.position)
            .CompareTo(
                Vector2.Distance(transform.position, b.transform.position)));

        if (currentTarget == null)
        {
            SelectTarget(visibleEnemies[0]);
            return;
        }

        int currentIndex = visibleEnemies.IndexOf(currentTarget);

        if (currentIndex == -1)
        {
            SelectTarget(visibleEnemies[0]);
            return;
        }

        int nextIndex = (currentIndex + 1) % visibleEnemies.Count;

        SelectTarget(visibleEnemies[nextIndex]);
    }

    // NOVO - projeta os 4 cantos da bounding box do sprite pro espaço de viewport
    // e testa se o retângulo resultante sobrepõe o retângulo da tela [0,1]x[0,1].
    // Diferente de TestPlanesAABB, esse teste não tem falso positivo: é uma
    // interseção de retângulos de fato, válida para câmera ortográfica sem rotação.
    private bool IsBoundsVisible(Bounds bounds, Camera cam)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        float z = bounds.center.z;

        Vector3[] corners =
        {
            new Vector3(min.x, min.y, z),
            new Vector3(max.x, min.y, z),
            new Vector3(min.x, max.y, z),
            new Vector3(max.x, max.y, z)
        };

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        bool anyInFront = false;

        foreach (Vector3 corner in corners)
        {
            Vector3 vp = cam.WorldToViewportPoint(corner);

            if (vp.z > 0f)
                anyInFront = true;

            minX = Mathf.Min(minX, vp.x);
            maxX = Mathf.Max(maxX, vp.x);
            minY = Mathf.Min(minY, vp.y);
            maxY = Mathf.Max(maxY, vp.y);
        }

        if (!anyInFront)
            return false;

        // Sobreposição de retângulos: [minX,maxX] com [0,1] E [minY,maxY] com [0,1]
        return maxX >= 0f && minX <= 1f && maxY >= 0f && minY <= 1f;
    }

    // NOVO - fallback simples baseado em ponto, usado só quando o inimigo
    // não tem SpriteRenderer (ex: um objeto lógico sem visual próprio).
    private bool IsPointVisible(Vector3 worldPos)
    {
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(worldPos);

        return viewportPos.x >= 0 &&
               viewportPos.x <= 1 &&
               viewportPos.y >= 0 &&
               viewportPos.y <= 1 &&
               viewportPos.z > 0;
    }

    public void ClearTarget()
    {
        if (currentTarget == null)
            return;

        currentTarget.GetComponent<SelectionCircle>()?.SetVisible(false, SelectionColor);

        currentTarget = null;
    }

    public bool CanCancel()
    {
        return currentTarget != null;
    }

    public void Cancel()
    {
        ClearTarget();
    }

    public int Priority => 10;
}