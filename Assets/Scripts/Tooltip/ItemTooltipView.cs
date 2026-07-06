using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text slotText;
    [SerializeField] private RectTransform statsContainer;
    [SerializeField] private GameObject statRowPrefab;
    [SerializeField] private TMP_Text descriptionText;

    // Resolvido sob demanda em vez de cacheado em Awake(): este componente é
    // adicionado via TooltipCanvasBuilder (Editor) e fica inativo na cena — Awake()
    // não tem garantia de já ter rodado quando Populate() é chamado pela primeira
    // vez em runtime (mesmo raciocínio do EnsureRefs() em SkillBarSlot).
    public RectTransform PanelRect => GetComponent<RectTransform>();

    public void Configure(
        TMP_Text nameText,
        TMP_Text rarityText,
        TMP_Text slotText,
        RectTransform statsContainer,
        GameObject statRowPrefab,
        TMP_Text descriptionText)
    {
        this.nameText = nameText;
        this.rarityText = rarityText;
        this.slotText = slotText;
        this.statsContainer = statsContainer;
        this.statRowPrefab = statRowPrefab;
        this.descriptionText = descriptionText;
    }

    public void Populate(ItemTooltipData data)
    {
        nameText.text = data.title;
        nameText.color = data.titleColor;

        rarityText.text = data.rarityLabel;

        bool hasSlot = !string.IsNullOrEmpty(data.slotLabel);
        slotText.gameObject.SetActive(hasSlot);

        if (hasSlot)
            slotText.text = $"Slot: {data.slotLabel}";

        ClearStats();

        foreach (var (label, value) in data.statRows)
        {
            GameObject row = Instantiate(statRowPrefab, statsContainer);
            row.GetComponent<StatRowUI>().Setup(label, value);
        }

        descriptionText.text = data.description;

        LayoutRebuilder.ForceRebuildLayoutImmediate(statsContainer);
        LayoutRebuilder.ForceRebuildLayoutImmediate(PanelRect);
    }

    private void ClearStats()
    {
        foreach (Transform child in statsContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
