using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.PlayerRuntime
{
public abstract class PlayerMoveComponent : MonoBehaviour
{
    public float moveSpeed { get => Read_moveSpeed_Core(); set => Write_moveSpeed_Core(value); }
    protected abstract float Read_moveSpeed_Core();
    protected abstract void Write_moveSpeed_Core(float value);
    public float jumpForce { get => Read_jumpForce_Core(); set => Write_jumpForce_Core(value); }
    protected abstract float Read_jumpForce_Core();
    protected abstract void Write_jumpForce_Core(float value);
    public float jumpCooldown { get => Read_jumpCooldown_Core(); set => Write_jumpCooldown_Core(value); }
    protected abstract float Read_jumpCooldown_Core();
    protected abstract void Write_jumpCooldown_Core(float value);
    public float coyoteTime { get => Read_coyoteTime_Core(); set => Write_coyoteTime_Core(value); }
    protected abstract float Read_coyoteTime_Core();
    protected abstract void Write_coyoteTime_Core(float value);
    public float bufferTime { get => Read_bufferTime_Core(); set => Write_bufferTime_Core(value); }
    protected abstract float Read_bufferTime_Core();
    protected abstract void Write_bufferTime_Core(float value);
    public float dashSpeed { get => Read_dashSpeed_Core(); set => Write_dashSpeed_Core(value); }
    protected abstract float Read_dashSpeed_Core();
    protected abstract void Write_dashSpeed_Core(float value);
    public float dashDuration { get => Read_dashDuration_Core(); set => Write_dashDuration_Core(value); }
    protected abstract float Read_dashDuration_Core();
    protected abstract void Write_dashDuration_Core(float value);
    public float dashCooldown { get => Read_dashCooldown_Core(); set => Write_dashCooldown_Core(value); }
    protected abstract float Read_dashCooldown_Core();
    protected abstract void Write_dashCooldown_Core(float value);
    public float lastDashTime { get => Read_lastDashTime_Core(); set => Write_lastDashTime_Core(value); }
    protected abstract float Read_lastDashTime_Core();
    protected abstract void Write_lastDashTime_Core(float value);
    public bool IsDashUnlocked { get => Read_IsDashUnlocked_Core(); }
    protected abstract bool Read_IsDashUnlocked_Core();
    public float airControlMin { get => Read_airControlMin_Core(); set => Write_airControlMin_Core(value); }
    protected abstract float Read_airControlMin_Core();
    protected abstract void Write_airControlMin_Core(float value);
    public float groundSmooth { get => Read_groundSmooth_Core(); set => Write_groundSmooth_Core(value); }
    protected abstract float Read_groundSmooth_Core();
    protected abstract void Write_groundSmooth_Core(float value);
    public float airSmooth { get => Read_airSmooth_Core(); set => Write_airSmooth_Core(value); }
    protected abstract float Read_airSmooth_Core();
    protected abstract void Write_airSmooth_Core(float value);
    public void TickMovementAudio() => Invoke_TickMovementAudio_13_Core();
    protected abstract void Invoke_TickMovementAudio_13_Core();
    public void ResetMovementAudio() => Invoke_ResetMovementAudio_14_Core();
    protected abstract void Invoke_ResetMovementAudio_14_Core();
    public void ApplyApexGravity() => Invoke_ApplyApexGravity_15_Core();
    protected abstract void Invoke_ApplyApexGravity_15_Core();
    public void ApplyFallSpeedLimit() => Invoke_ApplyFallSpeedLimit_16_Core();
    protected abstract void Invoke_ApplyFallSpeedLimit_16_Core();
    public void ExecuteMove(float speedMultiplier = 1f, float accelMultiplier = 1f) => Invoke_ExecuteMove_17_Core(speedMultiplier, accelMultiplier);
    protected abstract void Invoke_ExecuteMove_17_Core(float speedMultiplier, float accelMultiplier);
    public void ClearJumpRequest() => Invoke_ClearJumpRequest_18_Core();
    protected abstract void Invoke_ClearJumpRequest_18_Core();
    public void RequestJump() => Invoke_RequestJump_19_Core();
    protected abstract void Invoke_RequestJump_19_Core();
    public void ExecuteJump() => Invoke_ExecuteJump_20_Core();
    protected abstract void Invoke_ExecuteJump_20_Core();
    public bool IsIgnoringPlatform(Collider2D platform) => Invoke_IsIgnoringPlatform_21_Core(platform);
    protected abstract bool Invoke_IsIgnoringPlatform_21_Core(Collider2D platform);
    public void UpdateDropCollisions() => Invoke_UpdateDropCollisions_22_Core();
    protected abstract void Invoke_UpdateDropCollisions_22_Core();
    public int MaxAirJumps { get => Read_MaxAirJumps_Core(); }
    protected abstract int Read_MaxAirJumps_Core();
    public void ModifyMaxAirJumps(int amount) => Invoke_ModifyMaxAirJumps_24_Core(amount);
    protected abstract void Invoke_ModifyMaxAirJumps_24_Core(int amount);
    public void ExecuteDash() => Invoke_ExecuteDash_25_Core();
    protected abstract void Invoke_ExecuteDash_25_Core();
    public void SetDashUnlocked(bool unlocked) => Invoke_SetDashUnlocked_26_Core(unlocked);
    protected abstract void Invoke_SetDashUnlocked_26_Core(bool unlocked);
}
}
