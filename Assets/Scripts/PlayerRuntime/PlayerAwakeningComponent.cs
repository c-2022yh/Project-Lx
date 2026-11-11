using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.PlayerRuntime
{
public abstract class PlayerAwakeningComponent : MonoBehaviour
{
    public event Action OnAwakeningStarted { add => Add_OnAwakeningStarted_Core(value); remove => Remove_OnAwakeningStarted_Core(value); }
    protected abstract void Add_OnAwakeningStarted_Core(Action handler);
    protected abstract void Remove_OnAwakeningStarted_Core(Action handler);
    public event Action OnAwakeningEnded { add => Add_OnAwakeningEnded_Core(value); remove => Remove_OnAwakeningEnded_Core(value); }
    protected abstract void Add_OnAwakeningEnded_Core(Action handler);
    protected abstract void Remove_OnAwakeningEnded_Core(Action handler);
    public bool IsAwakened { get => Read_IsAwakened_Core(); }
    protected abstract bool Read_IsAwakened_Core();
    public bool IsAwakening { get => Read_IsAwakening_Core(); }
    protected abstract bool Read_IsAwakening_Core();
    public float IntroDuration { get => Read_IntroDuration_Core(); }
    protected abstract float Read_IntroDuration_Core();
    public float IntroElapsed { get => Read_IntroElapsed_Core(); }
    protected abstract float Read_IntroElapsed_Core();
    public bool IsDrainingEnergy { get => Read_IsDrainingEnergy_Core(); }
    protected abstract bool Read_IsDrainingEnergy_Core();
    public void TryAwaken(global::ProjectLx.PlayerRuntime.PlayerActor p) => Invoke_TryAwaken_7_Core(p);
    protected abstract void Invoke_TryAwaken_7_Core(global::ProjectLx.PlayerRuntime.PlayerActor p);
    public void SetAwakeningBlocked(bool blocked) => Invoke_SetAwakeningBlocked_8_Core(blocked);
    protected abstract void Invoke_SetAwakeningBlocked_8_Core(bool blocked);
    public void ModifyAwakeningDuration(float amount) => Invoke_ModifyAwakeningDuration_9_Core(amount);
    protected abstract void Invoke_ModifyAwakeningDuration_9_Core(float amount);
    public void BeginFinisher() => Invoke_BeginFinisher_10_Core();
    protected abstract void Invoke_BeginFinisher_10_Core();
    public void EndAwakening() => Invoke_EndAwakening_11_Core();
    protected abstract void Invoke_EndAwakening_11_Core();
}
}
