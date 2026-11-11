using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.PlayerRuntime
{
public abstract class PlayerActor : MonoBehaviour
{
    public Rigidbody2D rb { get => Read_rb_Core(); }
    protected abstract Rigidbody2D Read_rb_Core();
    public SpriteRenderer sr { get => Read_sr_Core(); }
    protected abstract SpriteRenderer Read_sr_Core();
    public bool isGrounded { get => Read_isGrounded_Core(); set => Write_isGrounded_Core(value); }
    protected abstract bool Read_isGrounded_Core();
    protected abstract void Write_isGrounded_Core(bool value);
    public Vector2 moveInput { get => Read_moveInput_Core(); set => Write_moveInput_Core(value); }
    protected abstract Vector2 Read_moveInput_Core();
    protected abstract void Write_moveInput_Core(Vector2 value);
    public bool isFacingRight { get => Read_isFacingRight_Core(); set => Write_isFacingRight_Core(value); }
    protected abstract bool Read_isFacingRight_Core();
    protected abstract void Write_isFacingRight_Core(bool value);
    public Transform groundCheck { get => Read_groundCheck_Core(); set => Write_groundCheck_Core(value); }
    protected abstract Transform Read_groundCheck_Core();
    protected abstract void Write_groundCheck_Core(Transform value);
    public LayerMask groundLayer { get => Read_groundLayer_Core(); set => Write_groundLayer_Core(value); }
    protected abstract LayerMask Read_groundLayer_Core();
    protected abstract void Write_groundLayer_Core(LayerMask value);
    public global::ProjectLx.PlayerRuntime.PlayerMoveComponent Move { get => Read_Move_Core(); }
    protected abstract global::ProjectLx.PlayerRuntime.PlayerMoveComponent Read_Move_Core();
    public global::ProjectLx.PlayerRuntime.PlayerAttackComponent Attack { get => Read_Attack_Core(); }
    protected abstract global::ProjectLx.PlayerRuntime.PlayerAttackComponent Read_Attack_Core();
    public global::ProjectLx.PlayerRuntime.PlayerSkillComponent Skill { get => Read_Skill_Core(); }
    protected abstract global::ProjectLx.PlayerRuntime.PlayerSkillComponent Read_Skill_Core();
    public global::ProjectLx.PlayerRuntime.PlayerHealthComponent Health { get => Read_Health_Core(); }
    protected abstract global::ProjectLx.PlayerRuntime.PlayerHealthComponent Read_Health_Core();
    public global::ProjectLx.PlayerRuntime.PlayerHitReactionComponent HitReaction { get => Read_HitReaction_Core(); }
    protected abstract global::ProjectLx.PlayerRuntime.PlayerHitReactionComponent Read_HitReaction_Core();
    public global::ProjectLx.PlayerRuntime.PlayerRespawnComponent Respawn { get => Read_Respawn_Core(); }
    protected abstract global::ProjectLx.PlayerRuntime.PlayerRespawnComponent Read_Respawn_Core();
    public global::ProjectLx.PlayerRuntime.PlayerEnergyComponent Energy { get => Read_Energy_Core(); }
    protected abstract global::ProjectLx.PlayerRuntime.PlayerEnergyComponent Read_Energy_Core();
    public global::ProjectLx.PlayerRuntime.PlayerAwakeningComponent Awakening { get => Read_Awakening_Core(); }
    protected abstract global::ProjectLx.PlayerRuntime.PlayerAwakeningComponent Read_Awakening_Core();
    public global::ProjectLx.PlayerRuntime.PlayerActionStateComponent ActionState { get => Read_ActionState_Core(); }
    protected abstract global::ProjectLx.PlayerRuntime.PlayerActionStateComponent Read_ActionState_Core();
    public global::ProjectLx.PlayerRuntime.PlayerAnimationComponent Animation { get => Read_Animation_Core(); }
    protected abstract global::ProjectLx.PlayerRuntime.PlayerAnimationComponent Read_Animation_Core();
    public global::ProjectLx.PlayerRuntime.PlayerStatsComponent Stats { get => Read_Stats_Core(); }
    protected abstract global::ProjectLx.PlayerRuntime.PlayerStatsComponent Read_Stats_Core();
    public void NotifyJumpStarted() => Invoke_NotifyJumpStarted_19_Core();
    protected abstract void Invoke_NotifyJumpStarted_19_Core();
    public bool TryGetGroundSupport(out Collider2D ground, out Vector2 point) => Invoke_TryGetGroundSupport_20_Core(out ground, out point);
    protected abstract bool Invoke_TryGetGroundSupport_20_Core(out Collider2D ground, out Vector2 point);
    public bool TryGetDropPlatforms(List<Collider2D> platforms) => Invoke_TryGetDropPlatforms_21_Core(platforms);
    protected abstract bool Invoke_TryGetDropPlatforms_21_Core(List<Collider2D> platforms);
    public void ClearBufferedActionInputs() => Invoke_ClearBufferedActionInputs_26_Core();
    protected abstract void Invoke_ClearBufferedActionInputs_26_Core();
    public void SetPhysicsFreeze(bool freeze) => Invoke_SetPhysicsFreeze_35_Core(freeze);
    protected abstract void Invoke_SetPhysicsFreeze_35_Core(bool freeze);
    public void SetInteractable(IInteractable interactable) => Invoke_SetInteractable_36_Core(interactable);
    protected abstract void Invoke_SetInteractable_36_Core(IInteractable interactable);
    public void ClearInteractable(IInteractable interactable) => Invoke_ClearInteractable_37_Core(interactable);
    protected abstract void Invoke_ClearInteractable_37_Core(IInteractable interactable);
}
}
