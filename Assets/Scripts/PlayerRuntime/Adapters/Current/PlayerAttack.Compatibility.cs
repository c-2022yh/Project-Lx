using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerAttack
{
    protected override void Invoke_ModifyBasicAttackDamageBonus_0_Core(float bonus) => ModifyBasicAttackDamageBonus(bonus);
    protected override void Add_AttackEffectCreated_Core(Action<AttackPattern, Vector3, Quaternion, float, AttackAudioContext> handler) => AttackEffectCreated += handler;
    protected override void Remove_AttackEffectCreated_Core(Action<AttackPattern, Vector3, Quaternion, float, AttackAudioContext> handler) => AttackEffectCreated -= handler;
    protected override void Add_OnAttackHit_Core(Action<IDamageable, DamageInfo> handler) => OnAttackHit += handler;
    protected override void Remove_OnAttackHit_Core(Action<IDamageable, DamageInfo> handler) => OnAttackHit -= handler;
    protected override void Invoke_NotifyAttackHit_3_Core(IDamageable target, DamageInfo damageInfo) => NotifyAttackHit(target, damageInfo);
    protected override void Invoke_ExecuteAttack_4_Core() => ExecuteAttack();
    protected override void Invoke_CancelCurrentAttack_5_Core() => CancelCurrentAttack();
    protected override bool Invoke_AddGroundAttackPattern_6_Core(AttackPattern pattern) => AddGroundAttackPattern(pattern);
    protected override bool Invoke_RemoveGroundAttackPattern_7_Core(AttackPattern pattern) => RemoveGroundAttackPattern(pattern);
}
