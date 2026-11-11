using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.Legacy
{
public partial class Player
{
    protected override Rigidbody2D Read_rb_Core() => rb;
    protected override SpriteRenderer Read_sr_Core() => sr;
    protected override bool Read_isGrounded_Core() => isGrounded;
    protected override void Write_isGrounded_Core(bool value) => isGrounded = value;
    protected override Vector2 Read_moveInput_Core() => moveInput;
    protected override void Write_moveInput_Core(Vector2 value) => moveInput = value;
    protected override bool Read_isFacingRight_Core() => isFacingRight;
    protected override void Write_isFacingRight_Core(bool value) => isFacingRight = value;
    protected override Transform Read_groundCheck_Core() => groundCheck;
    protected override void Write_groundCheck_Core(Transform value) => groundCheck = value;
    protected override LayerMask Read_groundLayer_Core() => groundLayer;
    protected override void Write_groundLayer_Core(LayerMask value) => groundLayer = value;
    protected override global::ProjectLx.PlayerRuntime.PlayerMoveComponent Read_Move_Core() => Move;
    protected override global::ProjectLx.PlayerRuntime.PlayerAttackComponent Read_Attack_Core() => Attack;
    protected override global::ProjectLx.PlayerRuntime.PlayerSkillComponent Read_Skill_Core() => Skill;
    protected override global::ProjectLx.PlayerRuntime.PlayerHealthComponent Read_Health_Core() => Health;
    protected override global::ProjectLx.PlayerRuntime.PlayerHitReactionComponent Read_HitReaction_Core() => HitReaction;
    protected override global::ProjectLx.PlayerRuntime.PlayerRespawnComponent Read_Respawn_Core() => Respawn;
    protected override global::ProjectLx.PlayerRuntime.PlayerEnergyComponent Read_Energy_Core() => Energy;
    protected override global::ProjectLx.PlayerRuntime.PlayerAwakeningComponent Read_Awakening_Core() => Awakening;
    protected override global::ProjectLx.PlayerRuntime.PlayerActionStateComponent Read_ActionState_Core() => ActionState;
    protected override global::ProjectLx.PlayerRuntime.PlayerAnimationComponent Read_Animation_Core() => Animation;
    protected override global::ProjectLx.PlayerRuntime.PlayerStatsComponent Read_Stats_Core() => Stats;
    protected override void Invoke_NotifyJumpStarted_19_Core() => NotifyJumpStarted();
    protected override bool Invoke_TryGetGroundSupport_20_Core(out Collider2D ground, out Vector2 point) => TryGetGroundSupport(out ground, out point);
    protected override bool Invoke_TryGetDropPlatforms_21_Core(List<Collider2D> platforms) => TryGetDropPlatforms(platforms);
    protected override void Invoke_ClearBufferedActionInputs_26_Core() => ClearBufferedActionInputs();
    protected override void Invoke_SetPhysicsFreeze_35_Core(bool freeze) => SetPhysicsFreeze(freeze);
    protected override void Invoke_SetInteractable_36_Core(IInteractable interactable) => SetInteractable(interactable);
    protected override void Invoke_ClearInteractable_37_Core(IInteractable interactable) => ClearInteractable(interactable);
}
}
