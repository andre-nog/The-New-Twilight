using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [Header("UI")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private RectTransform background;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Position")]
    [SerializeField] private Vector2 mouseOffset = new Vector2(20f, -20f);

    [Header("Stats")]
    [SerializeField] private Transform statsContainer;
    [SerializeField] private GameObject statRowPrefab;

    private RectTransform rectTransform;
    private RectTransform statsContainerRect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        rectTransform = GetComponent<RectTransform>();
        statsContainerRect = statsContainer.GetComponent<RectTransform>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        Hide();
    }

    private void Update()
    {
        if (Mouse.current == null || canvasGroup.alpha == 0)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 position = mousePos + mouseOffset;

        float width = background.rect.width;
        float height = background.rect.height;

        // Direita
        if (position.x + width > Screen.width)
            position.x = mousePos.x - width - mouseOffset.x;

        // Topo
        if (position.y + height > Screen.height)
            position.y = mousePos.y - height - mouseOffset.y;

        // Esquerda
        if (position.x < 0)
            position.x = 0;

        // Baixo
        if (position.y < 0)
            position.y = 0;

        rectTransform.position = position;
    }

    public void Show(ItemSO item)
    {
        canvasGroup.alpha = 1f;

        itemNameText.text = item.itemName;
        descriptionText.text = item.description;

        ClearStats();

        foreach (StatModifier modifier in item.modifiers)
        {
            GameObject row = Instantiate(statRowPrefab, statsContainer);

            row.GetComponent<StatRowUI>().Setup(
                GetStatName(modifier.stat),
                $"+{modifier.amount}"
            );
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(statsContainerRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(background);
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
    }

    private void ClearStats()
    {
        foreach (Transform child in statsContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private string GetStatName(StatType stat)
    {
        switch (stat)
        {
            case StatType.MaxHealth:
                return "Max Health";

            case StatType.MoveSpeed:
                return "Move Speed";

            case StatType.CriticalChance:
                return "Critical Chance";

            case StatType.CriticalDamage:
                return "Critical Damage";

            case StatType.AttackPower:
                return "Attack Power";

            case StatType.SpellPower:
                return "Spell Power";

            case StatType.MaxMana:
                return "Max Mana";

            case StatType.HealthRegen:
                return "Health Regen";

            case StatType.ManaRegen:
                return "Mana Regen";

            default:
                return stat.ToString();
        }
    }
}
