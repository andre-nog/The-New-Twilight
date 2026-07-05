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
            slots[i] = new SkillSlot { input = slotInputs[i], skill = skill };
        }
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

    // Atribui uma skill a um slot específico, sobrescrevendo o que estava lá — usado
    // pelo drag do Livro de Skills pra barra. A skill entra "fresca" (sem cooldown
    // pendente de quem ocupava o slot antes).
    public void SetSkillAt(int index, Skill skill)
    {
        if (index < 0 || index >= slots.Length)
            return;

        slots[index].skill = skill;
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
        (slots[indexA].cooldown, slots[indexB].cooldown) = (slots[indexB].cooldown, slots[indexA].cooldown);
        (slots[indexA].duration, slots[indexB].duration) = (slots[indexB].duration, slots[indexA].duration);
    }
}
