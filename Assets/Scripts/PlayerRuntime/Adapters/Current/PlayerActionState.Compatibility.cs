using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerActionState
{
    protected override PlayerActionType Read_CurrentState_Core() => CurrentState;
    protected override bool Read_isNormal_Core() => isNormal;
    protected override bool Read_isAttacking_Core() => isAttacking;
    protected override bool Read_isDashing_Core() => isDashing;
    protected override bool Read_isSkillActive_Core() => isSkillActive;
    protected override bool Read_isAwakening_Core() => isAwakening;
    protected override bool Read_isHitStunned_Core() => isHitStunned;
    protected override bool Read_isDead_Core() => isDead;
    protected override bool Invoke_IsBusy_8_Core() => IsBusy();
    protected override bool Invoke_CanMove_9_Core() => CanMove();
    protected override bool Invoke_CanJump_10_Core() => CanJump();
    protected override bool Invoke_CanAttack_11_Core() => CanAttack();
    protected override bool Invoke_CanDash_12_Core() => CanDash();
    protected override bool Invoke_CanSkill_13_Core() => CanSkill();
    protected override bool Invoke_CanAwakening_14_Core() => CanAwakening();
    protected override bool Invoke_CanTakeHit_15_Core() => CanTakeHit();
    protected override bool Invoke_CanDie_16_Core() => CanDie();
    protected override void Invoke_SetAttackRecoveryMovement_17_Core(bool allowed) => SetAttackRecoveryMovement(allowed);
    protected override void Invoke_ChangeState_18_Core(PlayerActionType newState) => ChangeState(newState);
    protected override void Invoke_EnterNormal_19_Core() => EnterNormal();
    protected override void Invoke_EnterAttack_20_Core() => EnterAttack();
    protected override void Invoke_EnterDash_21_Core() => EnterDash();
    protected override void Invoke_EnterSkill_22_Core() => EnterSkill();
    protected override void Invoke_EnterSkill_23_Core(bool allowMovement) => EnterSkill(allowMovement);
    protected override void Invoke_EnterAwakening_24_Core() => EnterAwakening();
    protected override void Invoke_EnterHitStun_25_Core() => EnterHitStun();
    protected override void Invoke_EnterDead_26_Core() => EnterDead();
    protected override void Invoke_RespawnToNormal_27_Core() => RespawnToNormal();
}
