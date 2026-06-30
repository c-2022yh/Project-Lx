using UnityEngine;

/// <summary>
/// UI 표시용 스킬 데이터.
/// 판용님 SkillData(abstract)가 스킬 동작을 정의하고,
/// SkillUIData는 인벤토리/슬롯 UI에 보여줄 정보를 담당.
/// 두 데이터는 skillId 또는 직접 참조로 매칭하면 될 것 같음
/// </summary>
[CreateAssetMenu(fileName = "New SkillUI", menuName = "Game Data/Skill UI Data")]
public class SkillUIData : ScriptableObject
{
    [Header("Identity")]
    public string skillId;            //  SkillData와 매칭할 식별자
    public string skillName;
    public Sprite icon;
    [TextArea(2, 4)]
    public string description;

    [Header("UI Display")]
    public float cooldownTime = 3f;   // UI 쿨타임 회전용
    public int soulCost = 1;          // 기력 게이지 소모 칸 수

    [Header("Slot Compatibility")]
    public SkillSlotType compatibleSlot = SkillSlotType.SlotA;

    //  SkillData와 연결할 때:
    // public SkillData linkedSkillData;  // 직접 참조 방식
    // 또는 skillId로 SkillRegistry에서 룩업
}