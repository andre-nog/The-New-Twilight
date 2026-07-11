using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Componente do slot do Livro de Skills. A hierarquia (Icon/Name/Locked Overlay/
// Pips/+ Button) vem pronta na cena via Assets/Editor/SkillBookCanvasBuilder.cs,
// que chama Configure() com a skill e as referências dos filhos.
//
// É origem de arrasto pra barra (o Livro nunca recebe drop de volta) E ponto de
// gasto de ponto de skill: o botão "+" aprende/upa a skill. Skills não-aprendidas
// (nível 0) ficam travadas — overlay ligado, ícone escurecido, e não iniciam arrasto.
// O estado "aprendida/nível" NÃO é baked: vem sempre da SkillProgression em runtime.
public class SkillBookSlot : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private static readonly Color LockedTint = new(0.22f, 0.26f, 0.34f, 1f);

    [SerializeField] private Image icon;
    [SerializeField] private Button plusButton;
    [SerializeField] private TMP_Text pipsText;
    [SerializeField] private GameObject lockedOverlay;

    // [field: SerializeField] para a referência que Configure() grava em build time
    // sobreviver à serialização até o runtime (sem isso, Skill volta a null ao carregar
    // a cena e o drag/refresh não acham a skill).
    [field: SerializeField] public Skill Skill { get; private set; }

    public void Configure(Skill skill, Image iconImage, Button plus, TMP_Text pips, GameObject locked)
    {
        Skill = skill;
        icon = iconImage;
        plusButton = plus;
        pipsText = pips;
        lockedOverlay = locked;
    }

    private void Awake()
    {
        if (plusButton != null)
            plusButton.onClick.AddListener(OnPlusClicked);
    }

    private void Start()
    {
        // Auto-inicializa mesmo antes do primeiro RefreshAll do SkillBookUI (ex.: se a
        // SkillProgression ainda não existe no load da cena, mostra tudo travado; o
        // Open()/OnProgressionChanged corrige quando ela passa a existir).
        Refresh();
    }

    private void OnDestroy()
    {
        if (plusButton != null)
            plusButton.onClick.RemoveListener(OnPlusClicked);
    }

    private bool IsLearned =>
        SkillProgression.Instance != null && SkillProgression.Instance.IsLearned(Skill);

    private void OnPlusClicked()
    {
        if (SkillProgression.Instance != null)
            SkillProgression.Instance.LearnOrUpgrade(Skill); // dispara OnProgressionChanged -> RefreshAll
    }

    // Atualiza o visual a partir do estado atual da progressão. Chamado pelo
    // SkillBookUI (RefreshAll) e no Start.
    public void Refresh()
    {
        int level = SkillProgression.Instance != null ? SkillProgression.Instance.GetLevel(Skill) : 0;
        bool learned = level >= 1;
        int max = Skill != null ? Skill.MaxLevel : 0;

        if (lockedOverlay != null)
            lockedOverlay.SetActive(!learned);

        if (icon != null)
        {
            // Sincroniza com Skill.icon toda vez (mesmo se virou null) — nunca fica
            // com um sprite antigo em cache, mesmo padrão de SkillBarSlot.Refresh().
            icon.sprite = Skill != null ? Skill.icon : null;
            icon.preserveAspect = true;
            icon.color = learned ? Color.white : LockedTint;
        }

        if (pipsText != null)
            pipsText.text = learned ? $"Lv {level}/{max}" : "Locked";

        if (plusButton != null)
            plusButton.interactable =
                SkillProgression.Instance != null && SkillProgression.Instance.CanLearnOrUpgrade(Skill);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // Skills não-aprendidas não podem ir pra barra.
        if (!IsLearned || Skill == null || SkillDragController.Instance == null)
            return;

        SkillDragController.Instance.BeginDrag(this, icon.sprite, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (SkillDragController.Instance == null || !SkillDragController.Instance.IsDragging)
            return;

        SkillDragController.Instance.UpdateGhostPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (SkillDragController.Instance != null)
            SkillDragController.Instance.EndDrag(eventData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Skill == null || TooltipManager.Instance == null)
            return;

        if (SkillDragController.Instance != null && SkillDragController.Instance.IsDragging)
            return;

        TooltipManager.Instance.ShowSkill(new SkillTooltipSource(Skill));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.Hide();
    }
}
