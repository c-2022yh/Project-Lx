using UnityEngine;
using System.Collections;

//프로젝트에서 스킬데이터 SO를 만들수 있도록 설정

public abstract class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public Sprite icon;

    [TextArea(2, 4)]
    public string description;

    [Header("Slot")]
    public SkillSlotType allowedSlots;

    [Header("Cost")]
    public float cooldownTime = 1f;
    public int soulCost = 0;

    [Header("Timing")]
    public float activeTime = 0.2f;
    
    //스킬 실행하는 가상 함수
    public abstract IEnumerator ProcessSkill(Player p);
}