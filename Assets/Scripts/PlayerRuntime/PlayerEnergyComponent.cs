using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.PlayerRuntime
{
public abstract class PlayerEnergyComponent : MonoBehaviour
{
    public event Action OnEnergyChanged { add => Add_OnEnergyChanged_Core(value); remove => Remove_OnEnergyChanged_Core(value); }
    protected abstract void Add_OnEnergyChanged_Core(Action handler);
    protected abstract void Remove_OnEnergyChanged_Core(Action handler);
    public float CurrentEnergy { get => Read_CurrentEnergy_Core(); }
    protected abstract float Read_CurrentEnergy_Core();
    public float MaxEnergy { get => Read_MaxEnergy_Core(); }
    protected abstract float Read_MaxEnergy_Core();
    public float EnergyRatio { get => Read_EnergyRatio_Core(); }
    protected abstract float Read_EnergyRatio_Core();
    public bool IsFull { get => Read_IsFull_Core(); }
    protected abstract bool Read_IsFull_Core();
    public bool HasOrb { get => Read_HasOrb_Core(); }
    protected abstract bool Read_HasOrb_Core();
    public EnergyOrb CurrentOrb { get => Read_CurrentOrb_Core(); }
    protected abstract EnergyOrb Read_CurrentOrb_Core();
    public bool IsEnergyGainBlocked { get => Read_IsEnergyGainBlocked_Core(); }
    protected abstract bool Read_IsEnergyGainBlocked_Core();
    public void AcquireOrb() => Invoke_AcquireOrb_8_Core();
    protected abstract void Invoke_AcquireOrb_8_Core();
    public void GainEnergy(float amount) => Invoke_GainEnergy_9_Core(amount);
    protected abstract void Invoke_GainEnergy_9_Core(float amount);
    public void ModifyEnergyGainMultiplier(float bonus) => Invoke_ModifyEnergyGainMultiplier_10_Core(bonus);
    protected abstract void Invoke_ModifyEnergyGainMultiplier_10_Core(float bonus);
    public bool HasEnergy(float requiredEnergy) => Invoke_HasEnergy_11_Core(requiredEnergy);
    protected abstract bool Invoke_HasEnergy_11_Core(float requiredEnergy);
    public void SetMaxEnergy(float newMaxEnergy) => Invoke_SetMaxEnergy_12_Core(newMaxEnergy);
    protected abstract void Invoke_SetMaxEnergy_12_Core(float newMaxEnergy);
    public void ModifyMaxEnergy(float amount) => Invoke_ModifyMaxEnergy_13_Core(amount);
    protected abstract void Invoke_ModifyMaxEnergy_13_Core(float amount);
    public void SetMaxEnergyLimit(object source, float limit) => Invoke_SetMaxEnergyLimit_14_Core(source, limit);
    protected abstract void Invoke_SetMaxEnergyLimit_14_Core(object source, float limit);
    public void RemoveMaxEnergyLimit(object source) => Invoke_RemoveMaxEnergyLimit_15_Core(source);
    protected abstract void Invoke_RemoveMaxEnergyLimit_15_Core(object source);
    public void ResetEnergy() => Invoke_ResetEnergy_16_Core();
    protected abstract void Invoke_ResetEnergy_16_Core();
    public void SetEnergyGainBlocked(bool blocked) => Invoke_SetEnergyGainBlocked_17_Core(blocked);
    protected abstract void Invoke_SetEnergyGainBlocked_17_Core(bool blocked);
    public bool CanSpendEnergy(float amount) => Invoke_CanSpendEnergy_18_Core(amount);
    protected abstract bool Invoke_CanSpendEnergy_18_Core(float amount);
    public bool TrySpendEnergy(float amount) => Invoke_TrySpendEnergy_19_Core(amount);
    protected abstract bool Invoke_TrySpendEnergy_19_Core(float amount);
    public bool DrainEnergy(float amount) => Invoke_DrainEnergy_20_Core(amount);
    protected abstract bool Invoke_DrainEnergy_20_Core(float amount);
}
}
