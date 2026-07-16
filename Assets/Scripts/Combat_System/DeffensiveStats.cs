using System;
using UnityEngine;

// 방어 관련 스탯
[Serializable] public class DefensiveStats
{
    [Header("Defense")]
    public float physicalDefense = 0f; //물리 방어력
    public float magicalDefense = 0f;  //마법 방어력
    
    [Header("Final Damage Modifier")]
    public float durability = 0f; //내구력, 피해 감소
}