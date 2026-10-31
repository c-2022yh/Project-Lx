
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

    [Header("Attack Damage Variation")]
    [Tooltip("Minimum damage multiplier rolled once when a player attack is created. Set both multipliers to 1 to disable variation.")]
    [SerializeField, Min(0f)] private float minDamageMultiplier = 0.9f;
    [Tooltip("Maximum damage multiplier. All targets hit by the same attack share the roll.")]
    [SerializeField, Min(0f)] private float maxDamageMultiplier = 1.1f;

    public float RollDamageMultiplier()
    {
        float minimum = Mathf.Max(0f, Mathf.Min(minDamageMultiplier, maxDamageMultiplier));
        float maximum = Mathf.Max(minimum, Mathf.Max(minDamageMultiplier, maxDamageMultiplier));
        return UnityEngine.Random.Range(minimum, maximum);
    }

    //공격 관련 스탯 접근자
    public OffensiveStats Offense => offense;
    public DefensiveStats Defense => defense;

}