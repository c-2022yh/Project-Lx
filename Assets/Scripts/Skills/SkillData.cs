using UnityEngine;
using System.Collections;

//프로젝트에서 스킬데이터 SO를 만들수 있도록 설정
[CreateAssetMenu(fileName = "NewSkill", menuName = "ScriptableObjects/SkillData")]

public abstract class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public float cooldownTime;
    public float manaCost;
    public int soulCost;
    public Sprite icon;
    [TextArea(2, 4)]
    public string description;

    [Header("Slot Compatibility")]
    public SkillSlotType compatibleSlot = SkillSlotType.SlotA;

    [Header("Movement (Dash)")]
    public float dashDistance; //돌진 거리
    public float duration;     //스킬 지속 시간 (돌진 및 베기 속도)

    [Header("Visual (Slash)")]
    public float startAngle;   //베기 시작 각도
    public float endAngle;     //베기 종료 각도
    public GameObject effectPrefab; //전용 이펙트 (필요 시)

    [Header("Combat")]
    public float damageMultiplier = 1.0f; //데미지 배율

    [Header("Indicator Settings")]
    public Vector2 hitBoxSize = new Vector2(1f, 1f);
    public Color indicatorColor = new Color(1f, 0.5f, 0.5f, 0.4f);
    public Vector3 indicatorOffset = new Vector3(1f, 0f, 0f);



    //스킬 실행하는 가상 함수
    public abstract IEnumerator ProcessSkill(
        Player p,
        SkillRangeIndicator indicator,
        GameObject weaponHandle,
        Collider2D swordCollider,
        float defaultAngle);
}