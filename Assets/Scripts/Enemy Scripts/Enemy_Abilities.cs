using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Runner de habilidades — independente do ataque básico (Enemy_Combat/
// Enemy_RangedBasicAttack, que implementam IEnemyBasicAttack). Um inimigo tem
// zero ou mais habilidades aqui; cada uma controla sua própria sequência
// inteira via EnemyAbility.Execute() (uma coroutine), então mecânicas de
// formas bem diferentes (área telegrafada, dash, cura, invocação, enrage...)
// convivem na mesma lista sem exigir um componente novo por mecânica — só um
// novo EnemyAbility subclass quando a forma for genuinely nova.
//
// Este componente só decide QUANDO uma habilidade dispara (a primeira fora de
// cooldown, com o inimigo engajado e fora do meio de um golpe básico) e
// segura o movimento normal enquanto ela roda — nunca COMO ela se comporta.
public class Enemy_Abilities : MonoBehaviour
{
    [Tooltip("Habilidades deste inimigo, em ordem de prioridade — a primeira pronta (fora de cooldown) é a que dispara.")]
    public List<EnemyAbility> abilities = new();

    private readonly Dictionary<EnemyAbility, float> cooldownTimers = new();

    private Enemy_Movement enemyMovement;
    private NavMeshAgent agent;
    private EnemyStats stats;
    private Coroutine activeRoutine;

    private void Awake()
    {
        enemyMovement = GetComponent<Enemy_Movement>();
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<EnemyStats>();

        foreach (EnemyAbility ability in abilities)
        {
            if (ability != null && !cooldownTimers.ContainsKey(ability))
                cooldownTimers[ability] = 0f;
        }
    }

    private void Update()
    {
        if (activeRoutine != null)
            return;

        Transform player = enemyMovement.GetPlayer();

        if (player == null)
        {
            ResetCooldowns();
            return;
        }

        // Só pausa o acúmulo durante o golpe básico, sem zerar o progresso —
        // senão um básico mais frequente que o cooldown da habilidade nunca
        // deixa o timer chegar lá enquanto o jogador fica no alcance de melee.
        if (enemyMovement.IsAttacking)
            return;

        for (int i = 0; i < abilities.Count; i++)
        {
            EnemyAbility ability = abilities[i];

            if (ability == null || !cooldownTimers.ContainsKey(ability))
                continue;

            cooldownTimers[ability] += Time.deltaTime;

            if (cooldownTimers[ability] < ability.cooldown)
                continue;

            cooldownTimers[ability] = 0f;
            activeRoutine = StartCoroutine(RunAbility(ability, player));
            return;
        }
    }

    private void ResetCooldowns()
    {
        foreach (EnemyAbility ability in new List<EnemyAbility>(cooldownTimers.Keys))
            cooldownTimers[ability] = 0f;
    }

    private IEnumerator RunAbility(EnemyAbility ability, Transform target)
    {
        enemyMovement.enabled = false;
        enemyMovement.SetIdlePose();

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        yield return ability.Execute(stats, transform, target);

        enemyMovement.RefreshAnimatorState();
        enemyMovement.enabled = true;
        activeRoutine = null;
    }
}
