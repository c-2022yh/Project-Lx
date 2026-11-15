using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.PlayerRuntime
{
public abstract class PlayerSkillComponent : MonoBehaviour
{
    public SkillData[] equippedSkills { get => Read_equippedSkills_Core(); set => Write_equippedSkills_Core(value); }
    protected abstract SkillData[] Read_equippedSkills_Core();
    protected abstract void Write_equippedSkills_Core(SkillData[] value);
    public SkillAudioSession GetAudioSession(SkillData skill) => Invoke_GetAudioSession_1_Core(skill);
    protected abstract SkillAudioSession Invoke_GetAudioSession_1_Core(SkillData skill);
    public const int SlotCount = 5;
    public const int ExclusiveSlot = 0;
    public const int FirstNormalSlot = 1;
    public IReadOnlyList<SkillData> OwnedSkills { get => Read_OwnedSkills_Core(); }
    protected abstract IReadOnlyList<SkillData> Read_OwnedSkills_Core();
    public IReadOnlyList<SkillData> EquippedSkills { get => Read_EquippedSkills_Core(); }
    protected abstract IReadOnlyList<SkillData> Read_EquippedSkills_Core();
    public SkillData ExclusiveSkill { get => Read_ExclusiveSkill_Core(); }
    protected abstract SkillData Read_ExclusiveSkill_Core();
    public bool IsUsingSkill { get => Read_IsUsingSkill_Core(); }
    protected abstract bool Read_IsUsingSkill_Core();
    public bool HasAwakeningSkillCharge { get => Read_HasAwakeningSkillCharge_Core(); }
    protected abstract bool Read_HasAwakeningSkillCharge_Core();
    public event Action OnSkillsChanged { add => Add_OnSkillsChanged_Core(value); remove => Remove_OnSkillsChanged_Core(value); }
    protected abstract void Add_OnSkillsChanged_Core(Action handler);
    protected abstract void Remove_OnSkillsChanged_Core(Action handler);
    public event Action<SkillData> OnSkillUsed { add => Add_OnSkillUsed_Core(value); remove => Remove_OnSkillUsed_Core(value); }
    protected abstract void Add_OnSkillUsed_Core(Action<SkillData> handler);
    protected abstract void Remove_OnSkillUsed_Core(Action<SkillData> handler);
    public event Action<int, float, float> OnSkillCooldownChanged { add => Add_OnSkillCooldownChanged_Core(value); remove => Remove_OnSkillCooldownChanged_Core(value); }
    protected abstract void Add_OnSkillCooldownChanged_Core(Action<int, float, float> handler);
    protected abstract void Remove_OnSkillCooldownChanged_Core(Action<int, float, float> handler);
    public bool HasSkill(SkillData skill) => Invoke_HasSkill_13_Core(skill);
    protected abstract bool Invoke_HasSkill_13_Core(SkillData skill);
    public SkillData GetEquippedSkill(int slotIndex) => Invoke_GetEquippedSkill_14_Core(slotIndex);
    protected abstract SkillData Invoke_GetEquippedSkill_14_Core(int slotIndex);
    public int GetEquippedSlot(SkillData skill) => Invoke_GetEquippedSlot_15_Core(skill);
    protected abstract int Invoke_GetEquippedSlot_15_Core(SkillData skill);
    public bool IsSkillEquipped(SkillData skill) => Invoke_IsSkillEquipped_16_Core(skill);
    protected abstract bool Invoke_IsSkillEquipped_16_Core(SkillData skill);
    public bool GrantSkill(SkillData skill, object source) => Invoke_GrantSkill_17_Core(skill, source);
    protected abstract bool Invoke_GrantSkill_17_Core(SkillData skill, object source);
    public bool RevokeSkill(SkillData skill, object source) => Invoke_RevokeSkill_18_Core(skill, source);
    protected abstract bool Invoke_RevokeSkill_18_Core(SkillData skill, object source);
    public bool SetExclusiveSkill(SkillData skill, object source) => Invoke_SetExclusiveSkill_19_Core(skill, source);
    protected abstract bool Invoke_SetExclusiveSkill_19_Core(SkillData skill, object source);
    public bool ClearExclusiveSkill(object source) => Invoke_ClearExclusiveSkill_20_Core(source);
    protected abstract bool Invoke_ClearExclusiveSkill_20_Core(object source);
    public bool CanEquipSkill(int slotIndex, SkillData skill, out string reason) => Invoke_CanEquipSkill_21_Core(slotIndex, skill, out reason);
    protected abstract bool Invoke_CanEquipSkill_21_Core(int slotIndex, SkillData skill, out string reason);
    public bool EquipSkill(int slotIndex, SkillData skill) => Invoke_EquipSkill_22_Core(slotIndex, skill);
    protected abstract bool Invoke_EquipSkill_22_Core(int slotIndex, SkillData skill);
    public bool UnequipSkill(int slotIndex) => Invoke_UnequipSkill_23_Core(slotIndex);
    protected abstract bool Invoke_UnequipSkill_23_Core(int slotIndex);
    public bool UnequipSkill(int slotIndex, SkillData expectedSkill) => Invoke_UnequipSkill_24_Core(slotIndex, expectedSkill);
    protected abstract bool Invoke_UnequipSkill_24_Core(int slotIndex, SkillData expectedSkill);
    public float GetCooldownRemaining(SkillData skill) => Invoke_GetCooldownRemaining_25_Core(skill);
    protected abstract float Invoke_GetCooldownRemaining_25_Core(SkillData skill);
    public float GetCooldownRemaining(int slotIndex) => Invoke_GetCooldownRemaining_26_Core(slotIndex);
    protected abstract float Invoke_GetCooldownRemaining_26_Core(int slotIndex);
    public float GetCooldownDuration(SkillData skill) => Invoke_GetCooldownDuration_27_Core(skill);
    protected abstract float Invoke_GetCooldownDuration_27_Core(SkillData skill);
    public bool IsOnCooldown(int slotIndex) => Invoke_IsOnCooldown_28_Core(slotIndex);
    protected abstract bool Invoke_IsOnCooldown_28_Core(int slotIndex);
    public void ResetCooldown(int slotIndex) => Invoke_ResetCooldown_29_Core(slotIndex);
    protected abstract void Invoke_ResetCooldown_29_Core(int slotIndex);
    public void ExecuteSkillX(global::ProjectLx.PlayerRuntime.PlayerActor p) => Invoke_ExecuteSkillX_30_Core(p);
    protected abstract void Invoke_ExecuteSkillX_30_Core(global::ProjectLx.PlayerRuntime.PlayerActor p);
    public void ExecuteSkillA(global::ProjectLx.PlayerRuntime.PlayerActor p) => Invoke_ExecuteSkillA_31_Core(p);
    protected abstract void Invoke_ExecuteSkillA_31_Core(global::ProjectLx.PlayerRuntime.PlayerActor p);
    public void ExecuteSkillS(global::ProjectLx.PlayerRuntime.PlayerActor p) => Invoke_ExecuteSkillS_32_Core(p);
    protected abstract void Invoke_ExecuteSkillS_32_Core(global::ProjectLx.PlayerRuntime.PlayerActor p);
    public void ExecuteSkillD(global::ProjectLx.PlayerRuntime.PlayerActor p) => Invoke_ExecuteSkillD_33_Core(p);
    protected abstract void Invoke_ExecuteSkillD_33_Core(global::ProjectLx.PlayerRuntime.PlayerActor p);
    public void ExecuteSkillF(global::ProjectLx.PlayerRuntime.PlayerActor p) => Invoke_ExecuteSkillF_34_Core(p);
    protected abstract void Invoke_ExecuteSkillF_34_Core(global::ProjectLx.PlayerRuntime.PlayerActor p);
    public void UseSkill(global::ProjectLx.PlayerRuntime.PlayerActor p, int slotIndex) => Invoke_UseSkill_35_Core(p, slotIndex);
    protected abstract void Invoke_UseSkill_35_Core(global::ProjectLx.PlayerRuntime.PlayerActor p, int slotIndex);
    public void CancelCurrentSkill() => Invoke_CancelCurrentSkill_36_Core();
    protected abstract void Invoke_CancelCurrentSkill_36_Core();
    public bool RegisterAwakeningSkill(ScorchedEarthSkillData skill) => Invoke_RegisterAwakeningSkill_37_Core(skill);
    protected abstract bool Invoke_RegisterAwakeningSkill_37_Core(ScorchedEarthSkillData skill);
    public void UnregisterAwakeningSkill(ScorchedEarthSkillData expectedSkill) => Invoke_UnregisterAwakeningSkill_38_Core(expectedSkill);
    protected abstract void Invoke_UnregisterAwakeningSkill_38_Core(ScorchedEarthSkillData expectedSkill);
    public void SetAwakeningSkillAvailable(bool available) => Invoke_SetAwakeningSkillAvailable_39_Core(available);
    protected abstract void Invoke_SetAwakeningSkillAvailable_39_Core(bool available);
    public bool TryUseAwakeningSkill(global::ProjectLx.PlayerRuntime.PlayerActor p) => Invoke_TryUseAwakeningSkill_40_Core(p);
    protected abstract bool Invoke_TryUseAwakeningSkill_40_Core(global::ProjectLx.PlayerRuntime.PlayerActor p);
    internal void BeginEquipmentChange() => Invoke_Internal_BeginEquipmentChange_Core();
    protected abstract void Invoke_Internal_BeginEquipmentChange_Core();
    internal void EndEquipmentChange() => Invoke_Internal_EndEquipmentChange_Core();
    protected abstract void Invoke_Internal_EndEquipmentChange_Core();
    internal void RevokeSource(object source) => Invoke_Internal_RevokeSource_Core(source);
    protected abstract void Invoke_Internal_RevokeSource_Core(object source);
    internal SkillData[] CaptureSlots() => Invoke_Internal_CaptureSlots_Core();
    protected abstract SkillData[] Invoke_Internal_CaptureSlots_Core();
    internal void RestoreSlots(SkillData[] snapshot) => Invoke_Internal_RestoreSlots_Core(snapshot);
    protected abstract void Invoke_Internal_RestoreSlots_Core(SkillData[] snapshot);
}
}
