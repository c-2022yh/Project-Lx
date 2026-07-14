
using UnityEngine;

//플레이어 스탯 관리 스크립트
public class PlayerStats : MonoBehaviour
{
    //공격 관련 스탯 
    [Header("Offensive Stats")]
    [SerializeField] private OffensiveStats offense = new();

    //방어 관련 스탯
    [Header("Defensive Stats")]
    [SerializeField] private DefensiveStats defense = new();

    //공격 관련 스탯 접근자
    public OffensiveStats Offense => offense;
    public DefensiveStats Defense => defense;

}