using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.PlayerRuntime
{
public abstract class PlayerAnimationComponent : MonoBehaviour
{
    public void PlayHit() => Invoke_PlayHit_0_Core();
    protected abstract void Invoke_PlayHit_0_Core();
    public void PlayDeath() => Invoke_PlayDeath_1_Core();
    protected abstract void Invoke_PlayDeath_1_Core();
    public void ResetOnRespawn() => Invoke_ResetOnRespawn_2_Core();
    protected abstract void Invoke_ResetOnRespawn_2_Core();
    public void PlaySkill(SkillAnimationType animationType) => Invoke_PlaySkill_3_Core(animationType);
    protected abstract void Invoke_PlaySkill_3_Core(SkillAnimationType animationType);
    public void PlayAwakening() => Invoke_PlayAwakening_4_Core();
    protected abstract void Invoke_PlayAwakening_4_Core();
    public void PlayDash(float holdTime) => Invoke_PlayDash_5_Core(holdTime);
    protected abstract void Invoke_PlayDash_5_Core(float holdTime);
    public void PlayAttack(int index) => Invoke_PlayAttack_6_Core(index);
    protected abstract void Invoke_PlayAttack_6_Core(int index);
}
}
