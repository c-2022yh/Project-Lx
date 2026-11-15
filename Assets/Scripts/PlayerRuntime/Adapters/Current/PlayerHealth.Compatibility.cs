using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerHealth
{
    protected override void Add_OnHealthChanged_Core(Action<float, float> handler) => OnHealthChanged += handler;
    protected override void Remove_OnHealthChanged_Core(Action<float, float> handler) => OnHealthChanged -= handler;
    protected override void Add_OnDied_Core(Action handler) => OnDied += handler;
    protected override void Remove_OnDied_Core(Action handler) => OnDied -= handler;
    protected override void Add_OnDamaged_Core(Action<float> handler) => OnDamaged += handler;
    protected override void Remove_OnDamaged_Core(Action<float> handler) => OnDamaged -= handler;
    protected override float Read_CurrentHealth_Core() => CurrentHealth;
    protected override float Read_MaxHealth_Core() => MaxHealth;
    protected override bool Read_IsDead_Core() => IsDead;
    protected override void Invoke_TakeDamage_6_Core(DamageInfo damageInfo, Vector2 hitDirection) => TakeDamage(damageInfo, hitDirection);
    protected override void Invoke_TakeDamage_7_Core(float damage, Vector2 damageSourcePosition) => TakeDamage(damage, damageSourcePosition);
    protected override void Invoke_Heal_8_Core(float amount) => Heal(amount);
    protected override void Invoke_FullHeal_9_Core() => FullHeal();
    protected override void Invoke_RestoreHealthOnRespawn_10_Core() => RestoreHealthOnRespawn();
    protected override void Invoke_AddMaxHealth_11_Core(int amount, bool healAddedAmount) => AddMaxHealth(amount, healAddedAmount);
    protected override void Invoke_RemoveMaxHealth_12_Core(int amount) => RemoveMaxHealth(amount);
}
