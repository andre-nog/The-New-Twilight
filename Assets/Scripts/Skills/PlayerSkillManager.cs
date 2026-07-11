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
    //
    // Público (não só chamado do próprio Awake): GameManager.RegisterPlayer roda dentro
    // do Awake() de PlayerHealth no MESMO GameObject recém-instanciado, e a ordem de
    // Awake() entre componentes não é garantida pela Unity — sem chamar isto explicitamente
    // antes de SkillBarUI.Rebind ler os slots, um respawn ocasionalmente lança NRE porque
    // PlayerSkillManager.Awake() (que constrói slots) ainda não rodou.
    public void EnsureSlotsBuilt()
    {
        if (slots != null)
            return;

        // A progressão precisa existir antes de semear a barra (só entram skills já
        // aprendidas). StatsManager (-100) já rodou o Awake aqui, então o roster da
        // classe está disponível pra SkillProgression montar.
        SkillProgression.EnsureCreated();

        // Loadout persiste o arranjo da barra entre respawns (ver SkillLoadout) — só
        // não tem dado ainda na primeiríssima vez que a barra é montada na sessão.
        SkillLoadout.EnsureCreated();

        // Precisa existir antes de qualquer skill ser castada — UseSkill consulta
        // CombatStateTracker.Instance pra skills com requiresOutOfCombat.
        CombatStateTracker.EnsureCreated();

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

        // Depois da primeira montagem, o loadout é a fonte de verdade (inclusive pra
        // slots vazios) — sem isso, todo respawn recomputaria a partir de
        // defaultSkills e perderia qualquer skill arrastada do Livro pra um slot fora
        // do kit padrão.
        bool useLoadout = SkillLoadout.Instance.Populated;

        for (int i = 0; i < count; i++)
        {
            Skill placed;

            if (useLoadout)
            {
                placed = SkillLoadout.Instance.GetSkill(i);
            }
            else
            {
                Skill skill = skills != null && i < skills.Count ? skills[i] : null;

                // Só posiciona na barra o que já está aprendido — no início do jogo isso
                // é apenas o Auto Attack. Skills não-aprendidas deixam o slot vazio até
                // o jogador aprendê-las e arrastá-las do Livro.
                bool learned = skill != null
                    && SkillProgression.Instance != null
                    && SkillProgression.Instance.IsLearned(skill);

                placed = learned ? skill : null;
            }

            slots[i] = new SkillSlot
            {
                input = slotInputs[i],
                skill = placed,
            };
        }

        if (!useLoadout)
            PlaceAutoLearnedSkills(currentClass);

        SyncLoadout();
    }

    // Grava o estado atual de todos os slots no loadout persistente — chamado depois
    // de qualquer mudança (montagem inicial, drag-and-drop, swap) pra que o próximo
    // respawn (nova instância de PlayerSkillManager) recupere o mesmo arranjo.
    private void SyncLoadout()
    {
        if (SkillLoadout.Instance == null)
            return;

        for (int i = 0; i < slots.Length; i++)
            SkillLoadout.Instance.Set(i, slots[i].skill);
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

            int targetIndex = skill.preferredDefaultSlot >= 0 && skill.preferredDefaultSlot < slots.Length
                ? skill.preferredDefaultSlot
                : 0;

            // Preferred slot already taken by another auto-learned skill — fall back
            // to the first empty slot instead of silently overwriting it.
            if (slots[targetIndex].skill != null)
            {
                targetIndex = -1;

                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].skill == null)
                    {
                        targetIndex = i;
                        break;
                    }
                }

                if (targetIndex < 0)
                    continue;
            }

            slots[targetIndex].skill = skill;
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

        // A classe pode ter mudado — re-deriva o roster de skills aprendíveis antes de
        // semear a barra (que só posiciona o que já está aprendido).
        if (SkillProgression.Instance != null)
            SkillProgression.Instance.RebuildRoster();

        // Kit antigo (do loadout persistido) não faz mais sentido pra classe nova —
        // força EnsureSlotsBuilt a re-derivar o layout inicial a partir dela.
        if (SkillLoadout.Instance != null)
            SkillLoadout.Instance.Clear();

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

        if (SkillProgression.Instance != null)
            SkillProgression.Instance.OnSkillLearned += HandleSkillLearned;
    }

    private void OnDisable()
    {
        if (SkillProgression.Instance != null)
            SkillProgression.Instance.OnSkillLearned -= HandleSkillLearned;

        foreach (SkillSlot slot in slots)
            slot.input.action.Disable();

        queuedSkill = null;
    }

    // Convenience default, not a lock: places a newly learned skill into its
    // preferred slot (Skill.preferredDefaultSlot) if that slot is still empty.
    // Skipped silently if the skill has no preference, is already placed
    // somewhere, or its preferred slot is taken — drag-and-drop still works
    // exactly as before either way.
    private void HandleSkillLearned(Skill skill)
    {
        if (skill == null || skill.preferredDefaultSlot < 0 || skill.preferredDefaultSlot >= slots.Length)
            return;

        if (FindSlot(skill) != null)
            return;

        SkillSlot target = slots[skill.preferredDefaultSlot];

        if (target.skill != null)
            return;

        target.skill = skill;

        SyncLoadout();

        if (SkillBarUI.Instance != null)
            SkillBarUI.Instance.RefreshAll();
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
        if (skill == null)
            return;

        if (GetRemainingCooldown(skill) > 0f && !skill.followTargetWhileOnCooldown)
            return;

        if (combat.isAttacking)
        {
            queuedSkill = skill; // guarda a intenção mais recente, sobrescrevendo qualquer fila anterior
            combat.NotifyCastAttempted();
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

    // Reflete o conteúdo atual de SkillLoadout nos slots AO VIVO desta barra, sem
    // recomputar nada a partir da classe (ao contrário de RebuildLoadout, que
    // reconstrói do zero) — chamado pelo SaveSystem depois que SkillLoadout.
    // RestoreState já atualizou o singleton, pra UI e slots convergirem com o save.
    public void ResyncFromLoadout()
    {
        EnsureSlotsBuilt();

        if (SkillLoadout.Instance == null)
            return;

        for (int i = 0; i < slots.Length; i++)
            slots[i].skill = SkillLoadout.Instance.GetSkill(i);

        if (SkillBarUI.Instance != null)
            SkillBarUI.Instance.RefreshAll();
    }

    public int SlotCount => slots.Length;

    public Skill GetSkillAt(int index)
    {
        return index >= 0 && index < slots.Length ? slots[index].skill : null;
    }

    // Permite castar um slot por índice sem passar pelo InputAction — usado pelo
    // clique do mouse no ícone da skill bar (SkillBarSlot.OnPointerClick), mesma
    // lógica de cooldown/fila/ataque do input de teclado.
    public void TryCastSlot(int index)
    {
        if (index < 0 || index >= slots.Length)
            return;

        TryCastOrQueue(slots[index].skill);
    }

    // Atribui uma skill a um slot, sobrescrevendo o que estava lá — usado pelo drag
    // do Livro de Skills pra barra, e com skill=null pra esvaziar um slot (drop fora
    // da barra). O ícone nunca é guardado à parte — vem sempre de Skill.icon, lido
    // direto na hora de exibir (ver SkillBarSlot.Refresh). A skill entra "fresca"
    // (sem cooldown pendente de quem ocupava o slot antes).
    //
    // Uma mesma skill não pode ficar em dois slots: se ela já estiver em outro slot,
    // esse slot é esvaziado antes da atribuição. Por isso quem chama deve refazer a
    // barra inteira (SkillBarUI.RefreshAll), não só o slot de destino.
    public void SetSkillAt(int index, Skill skill)
    {
        if (index < 0 || index >= slots.Length)
            return;

        float carriedCooldown = 0f;
        float carriedDuration = 0f;

        if (skill != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (i != index && slots[i].skill == skill)
                {
                    carriedCooldown = slots[i].cooldown;
                    carriedDuration = slots[i].duration;
                    ClearSlot(i);
                }
            }
        }

        slots[index].skill = skill;
        slots[index].cooldown = carriedCooldown;
        slots[index].duration = carriedDuration;

        SyncLoadout();
    }

    private void ClearSlot(int index)
    {
        slots[index].skill = null;
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

        SyncLoadout();
    }
}
