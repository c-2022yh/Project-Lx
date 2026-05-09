using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "ScriptableObjects/SkillData")]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public float cooldown;
    public float manaCost;
    public Sprite icon;

    [Header("Movement (Dash)")]
    public float dashDistance; //돌진 거리
    public float duration;     //스킬 지속 시간 (돌진 및 베기 속도)

    [Header("Visual (Slash)")]
    public float startAngle;   // 베기 시작 각도
    public float endAngle;     // 베기 종료 각도
    public GameObject effectPrefab; // 전용 이펙트 (필요 시)

    [Header("Combat")]
    public float damageMultiplier = 1.0f; // 데미지 배율
}