using System;
using UnityEngine;

//특정 공격 자체가 가진 고유 피해 설정
[Serializable]
public struct AttackDamageSpec
{
    public DamageType damageType;

    //공격력 / 마력 계수
    public float damageMultiplier;

    //치명타 가능 여부
    public bool canCritical;

    //이번 공격만 추가 관통
    public float bonusPenetration;

    //이번 공격만 추가 피해 증폭
    public float bonusDamageAmplification;




}