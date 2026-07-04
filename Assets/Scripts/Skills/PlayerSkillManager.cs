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

    public IReadOnlyList<Skill> EquippedSkills => equippedSkills;
    private Skill[] equippedSkills;

    private void Awake()
    {
        combat = GetComponent<Player_Combat>();

        EnsureSlotsBuilt();
    }

    // slots é privado e não-serializado — recompilar scripts no Editor (domain reload)
    // zera esse campo sem rodar Awake() de novo para objetos que já existiam na cena.
    // OnEnable roda nesse caso, então reconstruímos aqui também se precisar.
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

        int count = skills != null ? Mathf.Min(skills.Count, slotInputs.Count) : 0;

        if (skills != null && skills.Count > slotInputs.Count)
            Debug.LogWarning(
                $"PlayerSkillManager: classe '{currentClass.name}' tem {skills.Count} skills mas só {slotInputs.Count} inputs de slot — as últimas ficam de fora.",
                this);

        slots = new SkillSlot[count];
        equippedSkills = new Skill[count];

        for (int i = 0; i < count; i++)
        {
            slots[i] = new SkillSlot { input = slotInputs[i], skill = skills[i] };
            equippedSkills[i] = skills[i];
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
}
