using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.PlayerRuntime
{
public abstract class PlayerAttackComponent : MonoBehaviour
{
    public void ModifyBasicAttackDamageBonus(float bonus) => Invoke_ModifyBasicAttackDamageBonus_0_Core(bonus);
    protected abstract void Invoke_ModifyBasicAttackDamageBonus_0_Core(float bonus);
    public event Action<AttackPattern, Vector3, Quaternion, float, AttackAudioContext> AttackEffectCreated { add => Add_AttackEffectCreated_Core(value); remove => Remove_AttackEffectCreated_Core(value); }
    protected abstract void Add_AttackEffectCreated_Core(Action<AttackPattern, Vector3, Quaternion, float, AttackAudioContext> handler);
    protected abstract void Remove_AttackEffectCreated_Core(Action<AttackPattern, Vector3, Quaternion, float, AttackAudioContext> handler);
    public event Action<IDamageable, DamageInfo> OnAttackHit { add => Add_OnAttackHit_Core(value); remove => Remove_OnAttackHit_Core(value); }
    protected abstract void Add_OnAttackHit_Core(Action<IDamageable, DamageInfo> handler);
    protected abstract void Remove_OnAttackHit_Core(Action<IDamageable, DamageInfo> handler);
    public void NotifyAttackHit(IDamageable target, DamageInfo damageInfo) => Invoke_NotifyAttackHit_3_Core(target, damageInfo);
    protected abstract void Invoke_NotifyAttackHit_3_Core(IDamageable target, DamageInfo damageInfo);
    public void ExecuteAttack() => Invoke_ExecuteAttack_4_Core();
    protected abstract void Invoke_ExecuteAttack_4_Core();
    public void CancelCurrentAttack() => Invoke_CancelCurrentAttack_5_Core();
    protected abstract void Invoke_CancelCurrentAttack_5_Core();
    public bool AddGroundAttackPattern(AttackPattern pattern) => Invoke_AddGroundAttackPattern_6_Core(pattern);
    protected abstract bool Invoke_AddGroundAttackPattern_6_Core(AttackPattern pattern);
    public bool RemoveGroundAttackPattern(AttackPattern pattern) => Invoke_RemoveGroundAttackPattern_7_Core(pattern);
    protected abstract bool Invoke_RemoveGroundAttackPattern_7_Core(AttackPattern pattern);
}
}
