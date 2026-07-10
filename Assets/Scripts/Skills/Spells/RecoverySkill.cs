using System.Collections;
using UnityEngine;

// Self-cast channel: roots the player for channelDuration seconds while gradually
// restoring Health and Mana up to full. Uses Skill.executeEffectImmediately instead
// of an Animation Event because the channel outlives any attack clip —
// FinishAttacking and ReleaseMovement (normally fired by Animation Events) are
// called directly from the coroutine once the channel completes.
[CreateAssetMenu(menuName = "Skills/Recovery")]
public class RecoverySkill : Skill
{
    [Header("Recovery")]
    [Tooltip("Seconds the player stays rooted while channeling back to full Health and Mana.")]
    public float channelDuration = 5f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // maxCastDuration is the AttackTimeoutWatchdog failsafe (Player_Combat) — if
        // it's shorter than the channel, the watchdog force-ends the channel early
        // and releases the player before Health/Mana finish restoring.
        if (maxCastDuration < channelDuration)
            maxCastDuration = channelDuration + 0.5f;
    }
#endif

    public override float GetExpectedDamage(Player_Combat combat) => 0f;

    public override void ExecuteEffect(Player_Combat combat, in CastContext ctx)
    {
        combat.StartCoroutine(ChannelRoutine(combat));
    }

    private IEnumerator ChannelRoutine(Player_Combat combat)
    {
        StatsManager stats = StatsManager.Instance;

        int healthDeficit = stats.MaxHealth - stats.currentHealth;
        int manaDeficit = stats.MaxMana - stats.currentMana;

        float healthPerSecond = channelDuration > 0f ? healthDeficit / channelDuration : healthDeficit;
        float manaPerSecond = channelDuration > 0f ? manaDeficit / channelDuration : manaDeficit;

        // Fractional accumulators, same pattern as StatsManager.TickRegen — the
        // per-tick gain is rarely a whole number, so leftover fractions carry to
        // the next frame instead of being dropped.
        float healthAccumulator = 0f;
        float manaAccumulator = 0f;
        float elapsed = 0f;

        // Taking a hit, moving, or trying to cast another skill all cancel the
        // channel — whatever Health/Mana was already granted this tick stays
        // (nothing to revert), but the remaining gain and the full-restore snap
        // below are skipped.
        bool interrupted = false;
        void HandleDamaged() => interrupted = true;
        void HandleCastAttempted() => interrupted = true;

        PlayerHealth.OnPlayerDamaged += HandleDamaged;
        combat.OnCastAttemptedDuringAction += HandleCastAttempted;
        CastBarUI.Instance?.Show("Channeling");

        try
        {
            while (elapsed < channelDuration && !interrupted)
            {
                yield return null;

                if (combat.playerMovement.move.action.ReadValue<Vector2>() != Vector2.zero)
                    interrupted = true;

                float delta = Time.deltaTime;
                elapsed += delta;

                // Full at the start, drains to empty as the channel progresses.
                CastBarUI.Instance?.SetProgress(1f - elapsed / channelDuration);

                healthAccumulator += healthPerSecond * delta;
                int healthGain = Mathf.FloorToInt(healthAccumulator);

                if (healthGain > 0)
                {
                    healthAccumulator -= healthGain;
                    stats.ChangeHealth(healthGain);
                }

                manaAccumulator += manaPerSecond * delta;
                int manaGain = Mathf.FloorToInt(manaAccumulator);

                if (manaGain > 0)
                {
                    manaAccumulator -= manaGain;
                    stats.ChangeMana(manaGain);
                }
            }
        }
        finally
        {
            PlayerHealth.OnPlayerDamaged -= HandleDamaged;
            combat.OnCastAttemptedDuringAction -= HandleCastAttempted;
            CastBarUI.Instance?.Hide();
        }

        if (!interrupted)
        {
            // Snap to full — guarantees the "restores everything" promise even if
            // the per-tick rounding above left a fraction of a point short.
            stats.FullHeal();
            stats.RestoreFullMana();
        }

        combat.FinishAttacking();
        combat.ReleaseMovement();
    }
}
