using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkillManager : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Tecla de cada slot da barra — o slot N casa o input N com a skill N da classe atual. Adicionar um 4º slot = adicionar um input aqui e uma skill na classe.")]
    [SerializeField] private List<InputActionReference> slotInputs = new();

    private class SkillSlot
    {
        public InputActionReference input;
        public Skill skill;

        // Ícone exibido na barra. Para o kit da classe é Skill.icon; para skills
        // arrastadas do Livro é o sprite do slot de origem (SkillBookSlot.IconSprite),
        // que pode diferir de Skill.icon.
        public Sprite icon;
        public float cooldown;

        // Duração efetiva do cooldown que acabou de começar (já com Haste aplicado,
        // se a skill for afetada) — a UI usa isso pra normalizar o preenchimento,
        // em vez do skill.cooldown bruto.
        public float duration;
    }

    private Player_Combat combat;
    private SkillSlot[] slots;
    private Skill queuedSkill;

    private void Awake()
    {
        combat = GetComponent<Player_Combat>();

        EnsureSlotsBuilt();
    }

    // slots é privado e não-serializado — recompilar scripts no Editor (domain reload)
    // zera esse campo sem rodar Awake() de novo para objetos que já existiam na cena.
    // OnEnable roda nesse caso, então reconstruímos aqui também se precisar.
    //
    // A capacidade é slotInputs.Count (não mais limitada por quantas skills a classe
    // já tem) — slots além do kit padrão nascem vazios (skill = null), pra poder
    // receber uma skill via drag-and-drop do Livro de Skills em qualquer posição.
    private void EnsureSlotsBuilt()
    {
        if (slots != null)
            return;

        // A progressão precisa existir antes de semear a barra (só entram skills já
        // aprendidas). StatsManager (-100) já rodou o Awake aqui, então o roster da
        // classe está disponível pra SkillProgression montar.
        SkillProgression.EnsureCreated();

        // O kit vem da classe atual — o StatsManager é o dono da classe (roda antes,
        // DefaultExecutionOrder -100). Adicionar/trocar skill é editar o asset da
        // classe, não este código.
        ClassDefinitionSO currentClass = StatsManager.Instance != null
            ? StatsManager.Instance.CurrentClass
            : null;

        List<Skill> skills = currentClass != null ? currentClass.defaultSkills : null;

        if (skills != null && skills.Count > slotInputs.Count)
            Debug.LogWarning(
                $"PlayerSkillManager: classe '{currentClass.name}' tem {skills.Count} skills mas só {slotInputs.Count} inputs de slot — as últimas ficam de fora.",
                this);

        int count = slotInputs.Count;

        slots = new SkillSlot[count];

        for (int i = 0; i < count; i++)
        {
            Skill skill = skills != null && i < skills.Count ? skills[i] : null;

            // Só posiciona na barra o que já está aprendido — no início do jogo isso é
            // apenas o Auto Attack. Skills não-aprendidas deixam o slot vazio até o
            // jogador aprendê-las e arrastá-las do Livro.
            bool learned = skill != null
                && SkillProgression.Instance != null
                && SkillProgression.Instance.IsLearned(skill);

            Skill placed = learned ? skill : null;

            slots[i] = new SkillSlot
            {
                input = slotInputs[i],
                skill = placed,
                icon = ResolveIcon(placed),
            };
        }

        PlaceAutoLearnedSkills(currentClass);
    }

    // Skills autoLearnedAtStart (hoje só o Auto Attack) sempre aparecem na barra desde
    // o início, mesmo que defaultSkills não as liste na posição certa (ou não as liste
    // de jeito nenhum, só em learnableSkills) — reserva o Slot 1 (índice 0) pra elas se
    // ainda não estiverem posicionadas em nenhum slot. Varre defaultSkills e
    // learnableSkills juntos, mesma união que SkillProgression.BuildRoster usa.
    private void PlaceAutoLearnedSkills(ClassDefinitionSO currentClass)
    {
        if (currentClass == null || slots.Length == 0)
            return;

        HashSet<Skill> seen = new();

        PlaceAutoLearnedFrom(currentClass.defaultSkills, seen);
        PlaceAutoLearnedFrom(currentClass.learnableSkills, seen);
    }

    private void PlaceAutoLearnedFrom(List<Skill> skillList, HashSet<Skill> seen)
    {
        if (skillList == null)
            return;

        foreach (Skill skill in skillList)
        {
            if (skill == null || !skill.autoLearnedAtStart || !seen.Add(skill))
                continue;

            bool alreadyPlaced = false;

            foreach (SkillSlot slot in slots)
            {
                if (slot.skill == skill)
                {
                    alreadyPlaced = true;
                    break;
                }
            }

            if (alreadyPlaced)
                continue;

            slots[0].skill = skill;
            slots[0].icon = ResolveIcon(skill);
        }
    }

    // Ícone efetivo pra mostrar na barra: o mesmo sprite exibido no slot desta skill
    // no Livro (pode ter sido customizado no Inspector), com Skill.icon como fallback.
    private static Sprite ResolveIcon(Skill skill)
    {
        if (skill == null)
            return null;

        Sprite bookIcon = SkillBookUI.Instance != null ? SkillBookUI.Instance.GetIconFor(skill) : null;
        return bookIcon != null ? bookIcon : skill.icon;
    }

    // Hook da promoção de classe: depois de StatsManager.SetClass, chame isto para
    // reconstruir a barra a partir do novo kit.
    public void RebuildLoadout()
    {
        if (slots != null && isActiveAndEnabled)
        {
            foreach (SkillSlot slot in slots)
                slot.input.action.Disable();
        }

        slots = null;
        queuedSkill = null;

        // A classe pode ter mudado — re-deriva o roster de skills aprendíveis antes de
        // semear a barra (que só posiciona o que já está aprendido).
        if (SkillProgression.Instance != null)
            SkillProgression.Instance.RebuildRoster();

        EnsureSlotsBuilt();

        if (isActiveAndEnabled)
        {
            foreach (SkillSlot slot in slots)
                slot.input.action.Enable();
        }
    }

    private void OnEnable()
    {
        EnsureSlotsBuilt();

        foreach (SkillSlot slot in slots)
            slot.input.action.Enable();
    }

    private void OnDisable()
    {
        foreach (SkillSlot slot in slots)
            slot.input.action.Disable();

        queuedSkill = null;
    }

    private void Update()
    {
        foreach (SkillSlot slot in slots)
            TickCooldown(slot);

        foreach (SkillSlot slot in slots)
        {
            if (slot.input.action.WasPressedThisFrame())
                TryCastOrQueue(slot.skill);
        }

        if (!combat.isAttacking && queuedSkill != null)
        {
            Skill toCast = queuedSkill;
            queuedSkill = null;
            TryCastOrQueue(toCast);
        }
    }

    private static void TickCooldown(SkillSlot slot)
    {
        if (slot.cooldown > 0f)
            slot.cooldown = Mathf.Max(0f, slot.cooldown - Time.deltaTime);
    }

    private void TryCastOrQueue(Skill skill)
    {
        if (skill == null || GetRemainingCooldown(skill) > 0f)
            return;

        if (combat.isAttacking)
        {
            queuedSkill = skill; // guarda a intenção mais recente, sobrescrevendo qualquer fila anterior
            return;
        }

        skill.Cast(combat);
    }

    private SkillSlot FindSlot(Skill skill)
    {
        // Guarda explícita: sem isso, skill == null "encontraria" o primeiro slot
        // vazio (skill também null) em vez de reportar "não encontrado".
        if (skill == null)
            return null;

        foreach (SkillSlot slot in slots)
        {
            if (slot.skill == skill)
                return slot;
        }

        return null;
    }

    public float GetRemainingCooldown(Skill skill)
    {
        SkillSlot slot = FindSlot(skill);
        return slot != null ? slot.cooldown : float.PositiveInfinity;
    }

    // Duração efetiva do cooldown em andamento (já com Haste, se aplicável) — a UI
    // usa isso pra normalizar o preenchimento em vez do skill.cooldown bruto.
    public float GetCooldownDuration(Skill skill)
    {
        SkillSlot slot = FindSlot(skill);
        return slot != null ? slot.duration : 0f;
    }

    public void StartCooldown(Skill skill)
    {
        SkillSlot slot = FindSlot(skill);

        if (slot == null)
            return;

        // Cooldown não varia por nível — vem direto do campo fixo em Skill.
        float duration = skill.cooldown;

        if (skill.affectedByHaste)
            duration /= 1f + StatsManager.Instance.Haste;

        slot.cooldown = duration;
        slot.duration = duration;
    }

    public int SlotCount => slots.Length;

    public Skill GetSkillAt(int index)
    {
        return index >= 0 && index < slots.Length ? slots[index].skill : null;
    }

    public Sprite GetIconAt(int index)
    {
        return index >= 0 && index < slots.Length ? slots[index].icon : null;
    }

    // Atribui uma skill (com seu ícone) a um slot, sobrescrevendo o que estava lá —
    // usado pelo drag do Livro de Skills pra barra, e com skill=null pra esvaziar um
    // slot (drop fora da barra). A skill entra "fresca" (sem cooldown pendente de quem
    // ocupava o slot antes).
    //
    // Uma mesma skill não pode ficar em dois slots: se ela já estiver em outro slot,
    // esse slot é esvaziado antes da atribuição. Por isso quem chama deve refazer a
    // barra inteira (SkillBarUI.RefreshAll), não só o slot de destino.
    public void SetSkillAt(int index, Skill skill, Sprite icon)
    {
        if (index < 0 || index >= slots.Length)
            return;

        if (skill != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (i != index && slots[i].skill == skill)
                    ClearSlot(i);
            }
        }

        slots[index].skill = skill;
        slots[index].icon = icon;
        slots[index].cooldown = 0f;
        slots[index].duration = 0f;
    }

    private void ClearSlot(int index)
    {
        slots[index].skill = null;
        slots[index].icon = null;
        slots[index].cooldown = 0f;
        slots[index].duration = 0f;
    }

    // Troca o conteúdo de dois slots entre si — usado pra reorganizar a própria barra.
    // Cooldown/duration viajam junto com a skill (o cooldown pertence à skill que está
    // em progresso, não à posição do slot).
    public void SwapSkills(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Length || indexB < 0 || indexB >= slots.Length || indexA == indexB)
            return;

        (slots[indexA].skill, slots[indexB].skill) = (slots[indexB].skill, slots[indexA].skill);
        (slots[indexA].icon, slots[indexB].icon) = (slots[indexB].icon, slots[indexA].icon);
        (slots[indexA].cooldown, slots[indexB].cooldown) = (slots[indexB].cooldown, slots[indexA].cooldown);
        (slots[indexA].duration, slots[indexB].duration) = (slots[indexB].duration, slots[indexA].duration);
    }
}
