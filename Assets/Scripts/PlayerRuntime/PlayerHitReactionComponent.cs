using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.PlayerRuntime
{
public abstract class PlayerHitReactionComponent : MonoBehaviour
{
    public bool IsIgnoringEnemyContact { get => Read_IsIgnoringEnemyContact_Core(); }
    protected abstract bool Read_IsIgnoringEnemyContact_Core();
    public bool CanTakeDamage() => Invoke_CanTakeDamage_1_Core();
    protected abstract bool Invoke_CanTakeDamage_1_Core();
    public void PlayHitReaction(Vector2 damageSourcePosition) => Invoke_PlayHitReaction_2_Core(damageSourcePosition);
    protected abstract void Invoke_PlayHitReaction_2_Core(Vector2 damageSourcePosition);
    public void BeginEnemyContactIgnore() => Invoke_BeginEnemyContactIgnore_3_Core();
    protected abstract void Invoke_BeginEnemyContactIgnore_3_Core();
    public void EndEnemyContactIgnoreAfterDelay() => Invoke_EndEnemyContactIgnoreAfterDelay_4_Core();
    protected abstract void Invoke_EndEnemyContactIgnoreAfterDelay_4_Core();
    public void EndEnemyContactIgnoreAfterDelay(float delay) => Invoke_EndEnemyContactIgnoreAfterDelay_5_Core(delay);
    protected abstract void Invoke_EndEnemyContactIgnoreAfterDelay_5_Core(float delay);
    public void ResetReaction() => Invoke_ResetReaction_6_Core();
    protected abstract void Invoke_ResetReaction_6_Core();
    public void StartRespawnInvincible() => Invoke_StartRespawnInvincible_7_Core();
    protected abstract void Invoke_StartRespawnInvincible_7_Core();
}
}
