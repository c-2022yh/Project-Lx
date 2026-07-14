using UnityEngine;

//적 스탯 관리 스크립트
public class EnemyStats : MonoBehaviour
{
    [SerializeField]
    private DefensiveStats defense = new();

    public DefensiveStats Defense => defense;
}
