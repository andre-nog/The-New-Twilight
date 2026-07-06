using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTooltipView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text metaLineText;
    [SerializeField] private TMP_Text descriptionText;

    // Resolvido sob demanda em vez de cacheado em Awake(): este componente é
    // adicionado via TooltipCanvasBuilder (Editor) e fica inativo na cena — Awake()
    // não tem garantia de já ter rodado quando Populate() é chamado pela primeira
    // vez em runtime (mesmo raciocínio do EnsureRefs() em SkillBarSlot).
    public RectTransform PanelRect => GetComponent<RectTransform>();

    public void Configure(
        TMP_Text nameText,
        TMP_Text levelText,
        TMP_Text metaLineText,
        TMP_Text descriptionText)
    {
        this.nameText = nameText;
        this.levelText = levelText;
        this.metaLineText = metaLineText;
        this.descriptionText = descriptionText;
    }

    public void Populate(SkillTooltipData data)
    {
        nameText.text = data.title;
        levelText.text = $"Lvl {data.level}";

        bool hasMeta = !string.IsNullOrEmpty(data.metaLine);
        metaLineText.gameObject.SetActive(hasMeta);

        if (hasMeta)
            metaLineText.text = data.metaLine;

        descriptionText.text = data.description;

        LayoutRebuilder.ForceRebuildLayoutImmediate(PanelRect);
    }
}
