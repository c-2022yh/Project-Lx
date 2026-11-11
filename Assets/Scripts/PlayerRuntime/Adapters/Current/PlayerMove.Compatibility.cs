using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerMove
{
    protected override float Read_moveSpeed_Core() => moveSpeed;
    protected override void Write_moveSpeed_Core(float value) => moveSpeed = value;
    protected override float Read_jumpForce_Core() => jumpForce;
    protected override void Write_jumpForce_Core(float value) => jumpForce = value;
    protected override float Read_jumpCooldown_Core() => jumpCooldown;
    protected override void Write_jumpCooldown_Core(float value) => jumpCooldown = value;
    protected override float Read_coyoteTime_Core() => coyoteTime;
    protected override void Write_coyoteTime_Core(float value) => coyoteTime = value;
    protected override float Read_bufferTime_Core() => bufferTime;
    protected override void Write_bufferTime_Core(float value) => bufferTime = value;
    protected override float Read_dashSpeed_Core() => dashSpeed;
    protected override void Write_dashSpeed_Core(float value) => dashSpeed = value;
    protected override float Read_dashDuration_Core() => dashDuration;
    protected override void Write_dashDuration_Core(float value) => dashDuration = value;
    protected override float Read_dashCooldown_Core() => dashCooldown;
    protected override void Write_dashCooldown_Core(float value) => dashCooldown = value;
    protected override float Read_lastDashTime_Core() => lastDashTime;
    protected override void Write_lastDashTime_Core(float value) => lastDashTime = value;
    protected override bool Read_IsDashUnlocked_Core() => IsDashUnlocked;
    protected override float Read_airControlMin_Core() => airControlMin;
    protected override void Write_airControlMin_Core(float value) => airControlMin = value;
    protected override float Read_groundSmooth_Core() => groundSmooth;
    protected override void Write_groundSmooth_Core(float value) => groundSmooth = value;
    protected override float Read_airSmooth_Core() => airSmooth;
    protected override void Write_airSmooth_Core(float value) => airSmooth = value;
    protected override void Invoke_TickMovementAudio_13_Core() => TickMovementAudio();
    protected override void Invoke_ResetMovementAudio_14_Core() => ResetMovementAudio();
    protected override void Invoke_ApplyApexGravity_15_Core() => ApplyApexGravity();
    protected override void Invoke_ApplyFallSpeedLimit_16_Core() => ApplyFallSpeedLimit();
    protected override void Invoke_ExecuteMove_17_Core(float speedMultiplier, float accelMultiplier) => ExecuteMove(speedMultiplier, accelMultiplier);
    protected override void Invoke_ClearJumpRequest_18_Core() => ClearJumpRequest();
    protected override void Invoke_RequestJump_19_Core() => RequestJump();
    protected override void Invoke_ExecuteJump_20_Core() => ExecuteJump();
    protected override bool Invoke_IsIgnoringPlatform_21_Core(Collider2D platform) => IsIgnoringPlatform(platform);
    protected override void Invoke_UpdateDropCollisions_22_Core() => UpdateDropCollisions();
    protected override int Read_MaxAirJumps_Core() => MaxAirJumps;
    protected override void Invoke_ModifyMaxAirJumps_24_Core(int amount) => ModifyMaxAirJumps(amount);
    protected override void Invoke_ExecuteDash_25_Core() => ExecuteDash();
    protected override void Invoke_SetDashUnlocked_26_Core(bool unlocked) => SetDashUnlocked(unlocked);
}
