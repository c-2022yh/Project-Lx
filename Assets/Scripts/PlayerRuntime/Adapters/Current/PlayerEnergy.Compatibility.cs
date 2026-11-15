using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerEnergy
{
    protected override void Add_OnEnergyChanged_Core(Action handler) => OnEnergyChanged += handler;
    protected override void Remove_OnEnergyChanged_Core(Action handler) => OnEnergyChanged -= handler;
    protected override float Read_CurrentEnergy_Core() => CurrentEnergy;
    protected override float Read_MaxEnergy_Core() => MaxEnergy;
    protected override float Read_EnergyRatio_Core() => EnergyRatio;
    protected override bool Read_IsFull_Core() => IsFull;
    protected override bool Read_HasOrb_Core() => HasOrb;
    protected override EnergyOrb Read_CurrentOrb_Core() => CurrentOrb;
    protected override bool Read_IsEnergyGainBlocked_Core() => IsEnergyGainBlocked;
    protected override void Invoke_AcquireOrb_8_Core() => AcquireOrb();
    protected override void Invoke_GainEnergy_9_Core(float amount) => GainEnergy(amount);
    protected override void Invoke_ModifyEnergyGainMultiplier_10_Core(float bonus) => ModifyEnergyGainMultiplier(bonus);
    protected override bool Invoke_HasEnergy_11_Core(float requiredEnergy) => HasEnergy(requiredEnergy);
    protected override void Invoke_SetMaxEnergy_12_Core(float newMaxEnergy) => SetMaxEnergy(newMaxEnergy);
    protected override void Invoke_ModifyMaxEnergy_13_Core(float amount) => ModifyMaxEnergy(amount);
    protected override void Invoke_SetMaxEnergyLimit_14_Core(object source, float limit) => SetMaxEnergyLimit(source, limit);
    protected override void Invoke_RemoveMaxEnergyLimit_15_Core(object source) => RemoveMaxEnergyLimit(source);
    protected override void Invoke_ResetEnergy_16_Core() => ResetEnergy();
    protected override void Invoke_SetEnergyGainBlocked_17_Core(bool blocked) => SetEnergyGainBlocked(blocked);
    protected override bool Invoke_CanSpendEnergy_18_Core(float amount) => CanSpendEnergy(amount);
    protected override bool Invoke_TrySpendEnergy_19_Core(float amount) => TrySpendEnergy(amount);
    protected override bool Invoke_DrainEnergy_20_Core(float amount) => DrainEnergy(amount);
}
