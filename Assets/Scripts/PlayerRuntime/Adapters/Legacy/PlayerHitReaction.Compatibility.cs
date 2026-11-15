using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.Legacy
{
public partial class PlayerHitReaction
{
    protected override bool Read_IsIgnoringEnemyContact_Core() => IsIgnoringEnemyContact;
    protected override bool Invoke_CanTakeDamage_1_Core() => CanTakeDamage();
    protected override void Invoke_PlayHitReaction_2_Core(Vector2 damageSourcePosition) => PlayHitReaction(damageSourcePosition);
    protected override void Invoke_BeginEnemyContactIgnore_3_Core() => BeginEnemyContactIgnore();
    protected override void Invoke_EndEnemyContactIgnoreAfterDelay_4_Core() => EndEnemyContactIgnoreAfterDelay();
    protected override void Invoke_EndEnemyContactIgnoreAfterDelay_5_Core(float delay) => EndEnemyContactIgnoreAfterDelay(delay);
    protected override void Invoke_ResetReaction_6_Core() => ResetReaction();
    protected override void Invoke_StartRespawnInvincible_7_Core() => StartRespawnInvincible();
}
}
