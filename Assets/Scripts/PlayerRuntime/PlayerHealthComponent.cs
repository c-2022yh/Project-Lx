using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.PlayerRuntime
{
public abstract class PlayerHealthComponent : MonoBehaviour, IDamageable
{
    public event Action<float, float> OnHealthChanged { add => Add_OnHealthChanged_Core(value); remove => Remove_OnHealthChanged_Core(value); }
    protected abstract void Add_OnHealthChanged_Core(Action<float, float> handler);
    protected abstract void Remove_OnHealthChanged_Core(Action<float, float> handler);
    public event Action OnDied { add => Add_OnDied_Core(value); remove => Remove_OnDied_Core(value); }
    protected abstract void Add_OnDied_Core(Action handler);
    protected abstract void Remove_OnDied_Core(Action handler);
    public event Action<float> OnDamaged { add => Add_OnDamaged_Core(value); remove => Remove_OnDamaged_Core(value); }
    protected abstract void Add_OnDamaged_Core(Action<float> handler);
    protected abstract void Remove_OnDamaged_Core(Action<float> handler);
    public float CurrentHealth { get => Read_CurrentHealth_Core(); }
    protected abstract float Read_CurrentHealth_Core();
    public float MaxHealth { get => Read_MaxHealth_Core(); }
    protected abstract float Read_MaxHealth_Core();
    public bool IsDead { get => Read_IsDead_Core(); }
    protected abstract bool Read_IsDead_Core();
    public void TakeDamage(DamageInfo damageInfo, Vector2 hitDirection) => Invoke_TakeDamage_6_Core(damageInfo, hitDirection);
    protected abstract void Invoke_TakeDamage_6_Core(DamageInfo damageInfo, Vector2 hitDirection);
    public void TakeDamage(float damage, Vector2 damageSourcePosition) => Invoke_TakeDamage_7_Core(damage, damageSourcePosition);
    protected abstract void Invoke_TakeDamage_7_Core(float damage, Vector2 damageSourcePosition);
    public void Heal(float amount) => Invoke_Heal_8_Core(amount);
    protected abstract void Invoke_Heal_8_Core(float amount);
    public void FullHeal() => Invoke_FullHeal_9_Core();
    protected abstract void Invoke_FullHeal_9_Core();
    public void RestoreHealthOnRespawn() => Invoke_RestoreHealthOnRespawn_10_Core();
    protected abstract void Invoke_RestoreHealthOnRespawn_10_Core();
    public void AddMaxHealth(int amount, bool healAddedAmount = true) => Invoke_AddMaxHealth_11_Core(amount, healAddedAmount);
    protected abstract void Invoke_AddMaxHealth_11_Core(int amount, bool healAddedAmount);
    public void RemoveMaxHealth(int amount) => Invoke_RemoveMaxHealth_12_Core(amount);
    protected abstract void Invoke_RemoveMaxHealth_12_Core(int amount);
}
}
