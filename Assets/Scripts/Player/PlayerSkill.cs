using System;
using System.Collections;
using UnityEngine;

//플레이어 스킬을 관리하는 스크립트
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

    //스킬이 실제로 사용됐을 때 외부에 알리는 이벤트
    public event Action<SkillData> OnSkillUsed;

    //스킬 쿨타임
    private bool[] cooldowns = new bool[5];

    //폭주 전용 Q 스킬. 일반 장착 슬롯이나 쿨타임 배열에 넣지않음
    private ScorchedEarthSkillData awakeningSkill;
    private bool awakeningSkillAvailable;
    private Coroutine awakeningSkillRoutine;

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
        if (!p.ActionState.CanSkill()) return;

        //스킬 데이터를 가져와서 값이 없으면 리턴
        SkillData skill = equippedSkills[slotIndex];
        if (skill == null) return;

        //스킬별 사용 조건 검사
        if (!skill.CanUse(p)) return;

        //코루틴 돌림
        StartCoroutine(SkillRoutine(p, skill, slotIndex));
    }

    //스킬 코루틴
    private IEnumerator SkillRoutine(Player p, SkillData skill, int slotIndex)
    {
        //State 바꿈
        p.ActionState.EnterSkill();

        //스킬 사용 이벤트 발생
        OnSkillUsed?.Invoke(skill);

        yield return StartCoroutine(skill.ProcessSkill(p)); //실제 스킬 실행
        
        //State 되돌림
        if (p.ActionState.isSkillActive)
        {
            p.ActionState.EnterNormal();
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

    //특정 슬롯에 스킬 자동 장착 유물이펙트 관련 
    public bool EquipSkill(int slotIndex, SkillData skill)
    {
        if (skill == null) return false;
        if (slotIndex < 0 || slotIndex >= equippedSkills.Length) return false;

        //이미 같은 스킬이 장착되어 있어도 성공
        if (equippedSkills[slotIndex] == skill) return true;

        //다른 스킬이 들어 있으면 덮어쓰지 않음
        if (equippedSkills[slotIndex] != null)
        {
            return false;
        }

        equippedSkills[slotIndex] = skill;
        cooldowns[slotIndex] = false;

        Debug.Log($"[PlayerSkill] {slotIndex}번 슬롯에 {skill.name} 장착");

        // HUD에 스킬 아이콘을 표시하는 기능이 있다면 여기서 갱신
        // hudPanel.UpdateSkillSlot(slotIndex, skill);

        return true;
    }


    //특정 슬롯에서 유물이 지급한 스킬 자동 해제
    public bool UnequipSkill(int slotIndex, SkillData expectedSkill)
    {
        if (slotIndex < 0 || slotIndex >= equippedSkills.Length) return false;

        //해당 슬롯에 제거하려는 스킬이 실제로 들어 있는지 확인
        if (equippedSkills[slotIndex] != expectedSkill) return false;

        equippedSkills[slotIndex] = null;
        cooldowns[slotIndex] = false;

        Debug.Log($"[PlayerSkill] {slotIndex}번 슬롯 스킬 해제");

        // HUD에서 스킬 아이콘을 비우는 기능이 있다면 여기서 갱신
        // hudPanel.UpdateSkillSlot(slotIndex, null);

        return true;
    }

    //특정 슬롯 스킬 쿨타임 초기화
    public void ResetCooldown(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= cooldowns.Length) return;
        
        cooldowns[slotIndex] = false;

        Debug.Log( $"[PlayerSkill] {slotIndex}번 슬롯 쿨타임 초기화");

        // HUD에 초기화 기능이 생기면 여기서 함께 호출
        // hudPanel.ResetSkillCooldown(slotIndex);
    }

    //폭주 장착 시 Q 전용 스킬을 등록
    public bool RegisterAwakeningSkill(ScorchedEarthSkillData skill)
    {
        if (skill == null || (awakeningSkill != null && awakeningSkill != skill)) return false;
        awakeningSkill = skill;
        awakeningSkillAvailable = false;
        return true;
    }

    //폭주 해제 시 등록한 스킬만 제거하고 진행 중인 판정도 안전하게 취소
    public void UnregisterAwakeningSkill(ScorchedEarthSkillData expectedSkill)
    {
        if (awakeningSkill != expectedSkill) return;
        awakeningSkillAvailable = false;
        awakeningSkill = null;
        if (awakeningSkillRoutine != null)
        {
            StopCoroutine(awakeningSkillRoutine);
            awakeningSkillRoutine = null;
            Player owner = GetComponent<Player>();
            if (owner != null && owner.ActionState != null && owner.ActionState.isSkillActive)
                owner.ActionState.EnterNormal();
        }
    }

    //각성 시작 시 한 번의 사용권을 지급, 종료 시 회수
    public void SetAwakeningSkillAvailable(bool available)
    {
        awakeningSkillAvailable = awakeningSkill != null && available;
    }

    //각성 중 Q 입력에서만 호출. 피해량은 각성 종료 전 공격 스탯으로 확정
    public bool TryUseAwakeningSkill(Player p)
    {
        if (p == null || awakeningSkill == null || !awakeningSkillAvailable) return false;
        if (awakeningSkillRoutine != null || !awakeningSkill.CanUse(p)) return false;

        ScorchedEarthSkillData skill = awakeningSkill;
        DamageInfo damageInfo = skill.PrepareDamage(p);
        awakeningSkillAvailable = false;
        awakeningSkillRoutine = StartCoroutine(AwakeningSkillRoutine(p, skill, damageInfo));

        //한 번의 초토화를 시전한 즉시 각성 효과와 남은 시간을 정리
        p.Awakening.EndAwakening();
        return true;
    }

    //기존 일반 스킬과 같은 ActionState를 사용하되 별도 슬롯,쿨타임 적용x
    private IEnumerator AwakeningSkillRoutine(Player p, ScorchedEarthSkillData skill, DamageInfo damageInfo)
    {
        p.ActionState.EnterSkill();
        OnSkillUsed?.Invoke(skill);
        //중첩 IEnumerator로 기다려야 해제 시 외부 코루틴 하나만 멈춰도 판정 취소
        yield return skill.ProcessPreparedSkill(p, damageInfo);
        if (p != null && p.ActionState.isSkillActive) p.ActionState.EnterNormal();
        awakeningSkillRoutine = null;
    }

}
