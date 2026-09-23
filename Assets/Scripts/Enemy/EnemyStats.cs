using UnityEngine;

//적 스탯 관리 스크립트
public class EnemyStats : MonoBehaviour
{
    [Header("Offense")]
    [SerializeField]
    private OffensiveStats offense = new();

    [Header("Defense")]
    [SerializeField]
    private DefensiveStats defense = new();

    public OffensiveStats Offense => offense;
    public DefensiveStats Defense => defense;
}