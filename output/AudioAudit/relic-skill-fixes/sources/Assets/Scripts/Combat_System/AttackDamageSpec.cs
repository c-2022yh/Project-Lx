using System;
using UnityEngine;

//특정 공격 자체가 가진 고유 피해 설정
[Serializable]
public struct AttackDamageSpec
{
    public DamageType damageType;

    //공격력 / 마력 계수
    public float damageMultiplier;

    [Min(0f), Tooltip("Fixed damage added to the stat-scaled damage before critical hits, variance and mitigation. Set the multiplier to 0 for fixed damage only.")]
    public float flatDamage;

    //치명타 가능 여부
    public bool canCritical;

    //이번 공격만 추가 관통
    public float bonusPenetration;

    //이번 공격만 추가 피해 증폭
    public float bonusDamageAmplification;




}