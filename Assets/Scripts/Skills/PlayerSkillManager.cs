using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkillManager : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference skill1;
    public InputActionReference skill2;

    [Header("Skills")]
    public Skill autoAttack;
    public Skill powerStrike;

    private Player_Combat combat;

    private float skill1Cooldown;
    private float skill2Cooldown;

    private Skill queuedSkill; // NOVO: skill que foi pedida enquanto isAttacking == true

    private void Awake()
    {
        combat = GetComponent<Player_Combat>();
    }

    private void OnEnable()
    {
        skill1.action.Enable();
        skill2.action.Enable();
    }

    private void OnDisable()
    {
        skill1.action.Disable();
        skill2.action.Disable();
    }

    private void Update()
    {
        if (skill1Cooldown > 0)
            skill1Cooldown -= Time.deltaTime;

        if (skill2Cooldown > 0)
            skill2Cooldown -= Time.deltaTime;

        if (skill1.action.WasPressedThisFrame())
        {
            TryCastOrQueue(autoAttack, skill1Cooldown); // ALTERADO
        }

        if (skill2.action.WasPressedThisFrame())
        {
            TryCastOrQueue(powerStrike, skill2Cooldown); // ALTERADO
        }

        // NOVO: assim que a animação atual termina, dispara a skill que ficou na fila
        if (!combat.isAttacking && queuedSkill != null)
        {
            Skill toCast = queuedSkill;
            queuedSkill = null;
            toCast.Cast(combat);
        }
    }

    // NOVO: centraliza a decisão entre castar agora ou bufferizar
    private void TryCastOrQueue(Skill skill, float cooldownRemaining)
    {
        if (cooldownRemaining > 0)
            return; // ainda em cooldown, ignora o input normalmente

        if (combat.isAttacking)
        {
            queuedSkill = skill; // guarda a intenção mais recente, sobrescrevendo qualquer fila anterior
            return;
        }

        skill.Cast(combat);
    }

    public void StartCooldown(Skill skill)
    {
        if (skill == autoAttack)
            skill1Cooldown = autoAttack.cooldown;

        if (skill == powerStrike)
            skill2Cooldown = powerStrike.cooldown;
    }
}