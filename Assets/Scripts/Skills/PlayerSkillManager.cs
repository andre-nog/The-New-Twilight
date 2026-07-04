using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkillManager : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference skill1;
    public InputActionReference skill2;
    public InputActionReference skill3;

    [Header("Skills")]
    public Skill autoAttack;
    public Skill powerStrike;
    public Skill stomp;

    private class SkillSlot
    {
        public InputActionReference input;
        public Skill skill;
        public float cooldown;
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

        // Adicionar uma nova skill: adicione um par (input, skill) aqui.
        slots = new[]
        {
            new SkillSlot { input = skill1, skill = autoAttack },
            new SkillSlot { input = skill2, skill = powerStrike },
            new SkillSlot { input = skill3, skill = stomp }
        };

        equippedSkills = new Skill[slots.Length];
        for (int i = 0; i < slots.Length; i++)
            equippedSkills[i] = slots[i].skill;
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

    public void StartCooldown(Skill skill)
    {
        SkillSlot slot = FindSlot(skill);

        if (slot != null)
            slot.cooldown = skill.cooldown;
    }
}
