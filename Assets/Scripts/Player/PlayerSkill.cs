using System.Collections;
using UnityEngine;

public class PlayerSkill : MonoBehaviour
{
    [Header("Equipped Skills")]
    public SkillData[] equippedSkills = new SkillData[5];
    // 0: X Skill
    // 1: A Skill
    // 2: S Skill
    // 3: D Skill
    // 4: F Skill

    [Header("Dependencies")]
    [SerializeField] private HUDPanel hudPanel;

    //스킬 쿨타임
    private bool[] cooldowns = new bool[5];

    //스킬 연결
    public void ExecuteSkillX(Player p) { UseSkill(p, 0); }
    public void ExecuteSkillA(Player p) { UseSkill(p, 1); }
    public void ExecuteSkillS(Player p) { UseSkill(p, 2); }
    public void ExecuteSkillD(Player p) { UseSkill(p, 3); }
    public void ExecuteSkillF(Player p) { UseSkill(p, 4); }

    //스킬 사용
    public void UseSkill(Player p, int slotIndex)
    {
        //예외처리
        if (p == null) return;
        if (slotIndex < 0 || slotIndex >= equippedSkills.Length) return;
        
        //쿨타임이 안 돌았거나, 스킬을 사용할 수 없는 상태면 시전 x
        if (cooldowns[slotIndex]) return;
        if (!p.playerActionState.CanSkill()) return;

        //스킬 데이터를 가져와서 값이 없으면 리턴
        SkillData skill = equippedSkills[slotIndex];
        if (skill == null) return;

        //코루틴 돌림
        StartCoroutine(SkillRoutine(p, skill, slotIndex));
    }

    //스킬 코루틴
    private IEnumerator SkillRoutine(Player p, SkillData skill, int slotIndex)
    {
        //State 바꿈
        p.playerActionState.EnterSkill();

        yield return StartCoroutine(skill.ProcessSkill(p)); //실제 스킬 실행
        
        //State 되돌림
        if (p.playerActionState.isSkillActive)
        {
            p.playerActionState.EnterNormal();
        }

        //스킬 쿨 돌아가게
        cooldowns[slotIndex] = true;
        
        //UI 작동
        hudPanel.StartSkillCooldown(slotIndex, skill.cooldownTime);

        //스킬 쿨타임만큼 기다렸다가
        yield return new WaitForSeconds(skill.cooldownTime);
        //쿨타임 종료시키기
        cooldowns[slotIndex] = false;

    }

   
}