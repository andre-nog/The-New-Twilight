using System.Collections;
using UnityEngine;

// Habilidade especial de inimigo — independente do ataque básico (Enemy_Combat/
// Enemy_RangedBasicAttack). Um inimigo pode ter zero ou mais, cada uma um
// asset próprio (ver Enemy_Abilities). Só "cooldown" é genérico o bastante pra
// viver aqui: o resto (dano, windup, alcance, o que for) é decisão de cada
// habilidade concreta, porque as formas variam demais (área telegrafada, dash,
// cura, invocação, enrage...) pra caber num único conjunto de campos.
//
// Execute() é uma coroutine que a própria habilidade controla do início ao
// fim — o runner (Enemy_Abilities) só decide QUANDO chamar, nunca COMO a
// habilidade se comporta.
public abstract class EnemyAbility : ScriptableObject
{
    public string abilityName;

    [Tooltip("Tempo entre um uso desta habilidade e a próxima ficar disponível.")]
    public float cooldown = 3f;

    public abstract IEnumerator Execute(EnemyStats casterStats, Transform caster, Transform target);
}
