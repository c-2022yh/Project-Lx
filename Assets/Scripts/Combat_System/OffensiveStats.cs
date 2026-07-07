using System;
using UnityEngine;

//공격 관련 스탯 표기
[Serializable] public class OffensiveStats
{
    [Header("Attack")]
    public float physicalAttack = 10f;  //물리 공격력
    public float magicalAttack = 10f;   //마법 공격력

    [Header("Penetration")]
    public float physicalPenetration = 0f;  //물리 관통력
    public float magicalPenetration = 0f;   //마법 관통력

    [Header("Critical")]
    [Range(0f, 1f)]
    public float criticalChance = 0.05f; //치명타 확률

    public float criticalMultiplier = 1.5f; //치명타 배율
}