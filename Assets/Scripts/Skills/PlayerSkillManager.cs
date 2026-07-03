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
    private Player_Combat combat;
    private float skill1Cooldown;
    private float skill2Cooldown;
    private float skill3Cooldown;

    private Skill queuedSkill;

    private void Awake()
    {
        combat = GetComponent<Player_Combat>();
    }

    private void OnEnable()
    {
        skill1.action.Enable();
        skill2.action.Enable();
        skill3.action.Enable();
    }

    private void OnDisable()
    {
        skill1.action.Disable();
        skill2.action.Disable();
        skill3.action.Disable();
        queuedSkill = null;
    }

    private void Update()
    {
        TickCooldown(ref skill1Cooldown);
        TickCooldown(ref skill2Cooldown);
        TickCooldown(ref skill3Cooldown);

        if (skill1.action.WasPressedThisFrame())
        {
            TryCastOrQueue(autoAttack);
        }

        if (skill2.action.WasPressedThisFrame())
        {
            TryCastOrQueue(powerStrike);
        }

        if (skill3.action.WasPressedThisFrame())
        {
            TryCastOrQueue(stomp);
        }

        if (!combat.isAttacking && queuedSkill != null)
        {
            Skill toCast = queuedSkill;
            queuedSkill = null;
            TryCastOrQueue(toCast);
        }
    }

    private void TickCooldown(ref float cooldown)
    {
        if (cooldown > 0f)
            cooldown = Mathf.Max(0f, cooldown - Time.deltaTime);
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

    public float GetRemainingCooldown(Skill skill)
    {
        if (skill == autoAttack)
            return skill1Cooldown;

        if (skill == powerStrike)
            return skill2Cooldown;

        if (skill == stomp)
            return skill3Cooldown;

        return float.PositiveInfinity;
    }

    public void StartCooldown(Skill skill)
    {
        if (skill == autoAttack)
            skill1Cooldown = skill.cooldown;

        else if (skill == powerStrike)
            skill2Cooldown = skill.cooldown;

        else if (skill == stomp)
            skill3Cooldown = skill.cooldown;
    }
}
