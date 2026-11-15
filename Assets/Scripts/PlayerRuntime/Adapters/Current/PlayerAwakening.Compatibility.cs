using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerAwakening
{
    protected override void Add_OnAwakeningStarted_Core(Action handler) => OnAwakeningStarted += handler;
    protected override void Remove_OnAwakeningStarted_Core(Action handler) => OnAwakeningStarted -= handler;
    protected override void Add_OnAwakeningEnded_Core(Action handler) => OnAwakeningEnded += handler;
    protected override void Remove_OnAwakeningEnded_Core(Action handler) => OnAwakeningEnded -= handler;
    protected override bool Read_IsAwakened_Core() => IsAwakened;
    protected override bool Read_IsAwakening_Core() => IsAwakening;
    protected override float Read_IntroDuration_Core() => IntroDuration;
    protected override float Read_IntroElapsed_Core() => IntroElapsed;
    protected override bool Read_IsDrainingEnergy_Core() => IsDrainingEnergy;
    protected override void Invoke_TryAwaken_7_Core(global::ProjectLx.PlayerRuntime.PlayerActor p) => TryAwaken((Player)p);
    protected override void Invoke_SetAwakeningBlocked_8_Core(bool blocked) => SetAwakeningBlocked(blocked);
    protected override void Invoke_ModifyAwakeningDuration_9_Core(float amount) => ModifyAwakeningDuration(amount);
    protected override void Invoke_BeginFinisher_10_Core() => BeginFinisher();
    protected override void Invoke_EndAwakening_11_Core() => EndAwakening();
}
