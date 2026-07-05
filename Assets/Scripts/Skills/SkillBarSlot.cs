using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Componente do próprio slot da Skill Bar — a hierarquia (Icon/Cooldown/Key/Name/
// Cooldown Text) já vem pronta na cena via Assets/Editor/SkillBarCanvasBuilder.cs;
// aqui só encontramos os filhos, descobrimos nosso índice (a partir do rótulo "Key",
// que o builder já grava como "1".."9") e reagimos a drag/drop e ao tick de cooldown.
// É origem E destino de arrasto: dá pra tirar uma skill daqui (reorganizar a barra)
// e dá pra soltar aqui vindo do Livro de Skills ou de outro slot da barra.
public class SkillBarSlot : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    private Image icon;
    private TMP_Text nameText;
    private Image cooldownImage;
    private TMP_Text cooldownText;
    private bool refsResolved;

    public int SlotIndex { get; private set; }

    private PlayerSkillManager skillManager;

    // Busca preguiçosa em vez de Awake(): o Editor pode disparar Awake() no instante
    // em que o componente é adicionado (Assets/Editor/SkillBarCanvasBuilder.cs), antes
    // dos filhos (Icon/Cooldown/Key/Name/Cooldown Text) existirem. Resolver sob
    // demanda garante que isso só rode quando a hierarquia já está completa.
    private void EnsureRefs()
    {
        if (refsResolved)
            return;

        icon = transform.Find("Icon").GetComponent<Image>();
        nameText = transform.Find("Name").GetComponent<TMP_Text>();
        cooldownImage = transform.Find("Cooldown").GetComponent<Image>();
        cooldownText = transform.Find("Cooldown Text").GetComponent<TMP_Text>();
        SlotIndex = int.Parse(transform.Find("Key").GetComponent<TMP_Text>().text) - 1;

        refsResolved = true;
    }

    // Chamado por SkillBarUI.Build() — guarda o manager atual e já popula o visual.
    public void Initialize(PlayerSkillManager manager)
    {
        EnsureRefs();
        skillManager = manager;
        Refresh();
    }

    public void Refresh()
    {
        EnsureRefs();

        Skill skill = skillManager != null ? skillManager.GetSkillAt(SlotIndex) : null;
        bool hasIcon = skill != null && skill.icon != null;

        icon.sprite = hasIcon ? skill.icon : SkillBarUI.GetRuntimeSprite();
        icon.preserveAspect = hasIcon;
        icon.color = hasIcon
            ? Color.white
            : new Color(0.22f, 0.26f, 0.34f, 1f);

        nameText.text = skill != null ? skill.skillName : "Empty";
    }

    public void TickCooldown()
    {
        if (skillManager == null)
            return;

        EnsureRefs();

        Skill skill = skillManager.GetSkillAt(SlotIndex);

        float remaining = skillManager.GetRemainingCooldown(skill);
        float duration = skillManager.GetCooldownDuration(skill);
        bool coolingDown = skill != null && remaining > 0f && duration > 0f;

        cooldownImage.enabled = coolingDown;
        cooldownText.enabled = coolingDown;

        if (!coolingDown)
            return;

        cooldownImage.fillAmount = Mathf.Clamp01(remaining / duration);
        cooldownText.text = remaining >= 1f
            ? Mathf.CeilToInt(remaining).ToString()
            : remaining.ToString("0.0");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        EnsureRefs();

        if (skillManager == null || skillManager.GetSkillAt(SlotIndex) == null)
            return;

        SkillDragController.Instance.BeginDrag(this, icon.sprite, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!SkillDragController.Instance.IsDragging)
            return;

        SkillDragController.Instance.UpdateGhostPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SkillDragController.Instance.EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        SkillDragController.Instance.TryDrop(this);
    }
}
