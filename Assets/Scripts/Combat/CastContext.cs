using UnityEngine;

// Snapshot imutável de uma tentativa de cast — skill, alvo e multiplicador extra
// viajam juntos do UseSkill até os animation events. Precisa viver num campo do
// Player_Combat (animation events não recebem parâmetros), mas é escrito UMA vez
// por cast: nada de mutate-and-restore no meio do golpe, então um futuro
// DoT/multi-hit não corrompe o cast em andamento.
public readonly struct CastContext
{
    public readonly Skill Skill;
    public readonly GameObject Target;
    public readonly float ExtraMultiplier;

    public CastContext(Skill skill, GameObject target, float extraMultiplier = 1f)
    {
        Skill = skill;
        Target = target;
        ExtraMultiplier = extraMultiplier;
    }

    // Deriva um novo contexto em vez de mutar o atual (ex.: Stomp escalando pelo
    // Momentum consumido).
    public CastContext WithExtraMultiplier(float multiplier)
    {
        return new CastContext(Skill, Target, multiplier);
    }
}
