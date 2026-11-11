using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.PlayerRuntime
{
public abstract class PlayerActionStateComponent : MonoBehaviour
{
    public PlayerActionType CurrentState { get => Read_CurrentState_Core(); }
    protected abstract PlayerActionType Read_CurrentState_Core();
    public bool isNormal { get => Read_isNormal_Core(); }
    protected abstract bool Read_isNormal_Core();
    public bool isAttacking { get => Read_isAttacking_Core(); }
    protected abstract bool Read_isAttacking_Core();
    public bool isDashing { get => Read_isDashing_Core(); }
    protected abstract bool Read_isDashing_Core();
    public bool isSkillActive { get => Read_isSkillActive_Core(); }
    protected abstract bool Read_isSkillActive_Core();
    public bool isAwakening { get => Read_isAwakening_Core(); }
    protected abstract bool Read_isAwakening_Core();
    public bool isHitStunned { get => Read_isHitStunned_Core(); }
    protected abstract bool Read_isHitStunned_Core();
    public bool isDead { get => Read_isDead_Core(); }
    protected abstract bool Read_isDead_Core();
    public bool IsBusy() => Invoke_IsBusy_8_Core();
    protected abstract bool Invoke_IsBusy_8_Core();
    public bool CanMove() => Invoke_CanMove_9_Core();
    protected abstract bool Invoke_CanMove_9_Core();
    public bool CanJump() => Invoke_CanJump_10_Core();
    protected abstract bool Invoke_CanJump_10_Core();
    public bool CanAttack() => Invoke_CanAttack_11_Core();
    protected abstract bool Invoke_CanAttack_11_Core();
    public bool CanDash() => Invoke_CanDash_12_Core();
    protected abstract bool Invoke_CanDash_12_Core();
    public bool CanSkill() => Invoke_CanSkill_13_Core();
    protected abstract bool Invoke_CanSkill_13_Core();
    public bool CanAwakening() => Invoke_CanAwakening_14_Core();
    protected abstract bool Invoke_CanAwakening_14_Core();
    public bool CanTakeHit() => Invoke_CanTakeHit_15_Core();
    protected abstract bool Invoke_CanTakeHit_15_Core();
    public bool CanDie() => Invoke_CanDie_16_Core();
    protected abstract bool Invoke_CanDie_16_Core();
    public void SetAttackRecoveryMovement(bool allowed) => Invoke_SetAttackRecoveryMovement_17_Core(allowed);
    protected abstract void Invoke_SetAttackRecoveryMovement_17_Core(bool allowed);
    public void ChangeState(PlayerActionType newState) => Invoke_ChangeState_18_Core(newState);
    protected abstract void Invoke_ChangeState_18_Core(PlayerActionType newState);
    public void EnterNormal() => Invoke_EnterNormal_19_Core();
    protected abstract void Invoke_EnterNormal_19_Core();
    public void EnterAttack() => Invoke_EnterAttack_20_Core();
    protected abstract void Invoke_EnterAttack_20_Core();
    public void EnterDash() => Invoke_EnterDash_21_Core();
    protected abstract void Invoke_EnterDash_21_Core();
    public void EnterSkill() => Invoke_EnterSkill_22_Core();
    protected abstract void Invoke_EnterSkill_22_Core();
    public void EnterSkill(bool allowMovement) => Invoke_EnterSkill_23_Core(allowMovement);
    protected abstract void Invoke_EnterSkill_23_Core(bool allowMovement);
    public void EnterAwakening() => Invoke_EnterAwakening_24_Core();
    protected abstract void Invoke_EnterAwakening_24_Core();
    public void EnterHitStun() => Invoke_EnterHitStun_25_Core();
    protected abstract void Invoke_EnterHitStun_25_Core();
    public void EnterDead() => Invoke_EnterDead_26_Core();
    protected abstract void Invoke_EnterDead_26_Core();
    public void RespawnToNormal() => Invoke_RespawnToNormal_27_Core();
    protected abstract void Invoke_RespawnToNormal_27_Core();
}
}
