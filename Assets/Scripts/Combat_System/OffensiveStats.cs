using System;
using UnityEngine;

//공격 관련 스탯 표기
[Serializable] public class OffensiveStats
{
    [Header("Attack")]
    public float physicalAttack = 1f;  //물리 공격력
    public float magicalAttack = 1f;   //마법 공격력

    [Header("Penetration")]
    public float physicalPenetration = 0f;  //물리 관통력
    public float magicalPenetration = 0f;   //마법 관통력

    [Header("Critical")]
    [Range(0f, 1f)]
    public float criticalChance = 0.0f;    //크리티컬 확률
    public float criticalMultiplier = 1.5f; //크리티컬 데미지
    
    [Header("Final Damage Modifier")]
    public float damageAmplification = 0f;  //피해 증폭

}