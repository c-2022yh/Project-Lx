using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.Legacy
{
public partial class PlayerAnimation
{
    protected override void Invoke_PlayHit_0_Core() => PlayHit();
    protected override void Invoke_PlayDeath_1_Core() => PlayDeath();
    protected override void Invoke_ResetOnRespawn_2_Core() => ResetOnRespawn();
    protected override void Invoke_PlaySkill_3_Core(SkillAnimationType animationType) => PlaySkill(animationType);
    protected override void Invoke_PlayAwakening_4_Core() => PlayAwakening();
    protected override void Invoke_PlayDash_5_Core(float holdTime) => PlayDash(holdTime);
    protected override void Invoke_PlayAttack_6_Core(int index) => PlayAttack(index);
}
}
