using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkill : MonoBehaviour
{
    [Header("Equipped Skills - 0:X, 1:A, 2:S, 3:D, 4:F")]
    public SkillData[] equippedSkills = new SkillData[5];

    [Header("Starting Skills - Test")]
    [SerializeField] private List<SkillData> startingSkills = new();

    [Header("Dependencies")]
    [SerializeField] private HUDPanel hudPanel;

    private readonly List<SkillData> ownedSkills = new();
    private readonly Dictionary<SkillData, HashSet<object>> skillSources = new();
    private readonly Dictionary<SkillData, float> cooldownEnds = new();

    private Player player;
    private SkillData exclusiveSkill;
    private object exclusiveSource;
    private SkillData activeSkill;
    private bool isInitialized;
    private bool isChangingEquipment;
    private bool skillsChanged;
    private readonly bool[] cooldownChanged = new bool[5];

    private ScorchedEarthSkillData awakeningSkill;
    private bool awakeningSkillAvailable;
    private Coroutine awakeningSkillRoutine;

    public const int SlotCount = 5;
    public const int ExclusiveSlot = 0;
    public const int FirstNormalSlot = 1;

    public IReadOnlyList<SkillData> OwnedSkills
    {
        get { Initialize(); return ownedSkills.AsReadOnly(); }
    }

    public IReadOnlyList<SkillData> EquippedSkills
    {
        get { Initialize(); return Array.AsReadOnly(equippedSkills); }
    }

    public SkillData ExclusiveSkill => exclusiveSkill;
    public bool IsUsingSkill => activeSkill != null || awakeningSkillRoutine != null;

    public event Action OnSkillsChanged;
    public event Action<SkillData> OnSkillUsed;
    public event Action<int, float, float> OnSkillCooldownChanged;

    //초기 데이터와 필요한 컴포넌트 연결
    private void Awake()
    {
        Initialize();
    }

    //시작 일반 스킬을 한 번만 등록하고 X는 유물이 지급하도록 초기화
    private void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;
        player = GetComponent<Player>();

        if (equippedSkills == null) equippedSkills = new SkillData[SlotCount];
        if (equippedSkills.Length != SlotCount) Array.Resize(ref equippedSkills, SlotCount);
        equippedSkills[ExclusiveSlot] = null;

        foreach (SkillData skill in startingSkills) AddSkillSource(skill, this);

        for (int i = FirstNormalSlot; i < SlotCount; i++)
        {
            SkillData skill = equippedSkills[i];
            if (skill == null) continue;
            if (Array.IndexOf(equippedSkills, skill) != i)
            {
                equippedSkills[i] = null;
                continue;
            }
            AddSkillSource(skill, this);
        }
    }

    //일반 스킬 또는 현재 X 전용 스킬 보유 여부 확인
    public bool HasSkill(SkillData skill)
    {
        Initialize();
        return skill != null && (skill == exclusiveSkill || ownedSkills.Contains(skill));
    }

    //슬롯의 스킬 조회. 0=X, 1=A, 2=S, 3=D, 4=F
    public SkillData GetEquippedSkill(int slotIndex)
    {
        Initialize();
        if (!IsValidSlot(slotIndex)) return null;
        return slotIndex == ExclusiveSlot ? exclusiveSkill : equippedSkills[slotIndex];
    }

    //스킬의 현재 슬롯 번호 반환. 장착되지 않았으면 -1
    public int GetEquippedSlot(SkillData skill)
    {
        Initialize();
        if (skill == null) return -1;
        if (skill == exclusiveSkill) return ExclusiveSlot;
        for (int i = FirstNormalSlot; i < SlotCount; i++)
        {
            if (equippedSkills[i] == skill) return i;
        }
        return -1;
    }

    //스킬 장착 여부 확인
    public bool IsSkillEquipped(SkillData skill) => GetEquippedSlot(skill) >= 0;

    //X와 ASDF를 포함한 슬롯 번호 유효성 확인
    private bool IsValidSlot(int slotIndex) => slotIndex >= 0 && slotIndex < SlotCount;

    //일반 스킬 지급권 등록. 새로 보유하면 A→S→D→F 첫 빈칸에 장착하며 꽉 차면 보유만 유지
    public bool GrantSkill(SkillData skill, object source)
    {
        Initialize();
        if (skill == null || source == null || skill == exclusiveSkill) return false;
        if (IsUsingSkill) return false;

        bool wasOwned = ownedSkills.Contains(skill);
        bool added = AddSkillSource(skill, source);
        if (!added) return true;

        if (!wasOwned) TryAutoEquipSkill(skill);
        NotifySkillsChanged();
        return true;
    }

    //지급 출처를 기록하고 같은 출처의 중복 지급 방지
    private bool AddSkillSource(SkillData skill, object source)
    {
        if (skill == null || source == null) return false;
        if (!skillSources.TryGetValue(skill, out HashSet<object> sources))
        {
            sources = new HashSet<object>();
            skillSources.Add(skill, sources);
            ownedSkills.Add(skill);
        }
        return sources.Add(source);
    }

    //기존 배치를 유지하면서 첫 빈 ASDF 슬롯에 자동 장착
    private void TryAutoEquipSkill(SkillData skill)
    {
        if (IsSkillEquipped(skill)) return;
        for (int i = FirstNormalSlot; i < SlotCount; i++)
        {
            if (equippedSkills[i] != null) continue;
            equippedSkills[i] = skill;
            NotifyCooldown(i);
            return;
        }
    }

    //해당 지급권만 회수. 마지막 지급권이면 보유와 슬롯에서 제거하며 쿨타임 유지
    public bool RevokeSkill(SkillData skill, object source)
    {
        Initialize();
        if (skill == null || source == null || IsUsingSkill) return false;
        if (!skillSources.TryGetValue(skill, out HashSet<object> sources)) return false;
        if (!sources.Remove(source)) return false;

        if (sources.Count == 0)
        {
            for (int i = FirstNormalSlot; i < SlotCount; i++)
            {
                if (equippedSkills[i] == skill) ClearSlot(i);
            }
            skillSources.Remove(skill);
            ownedSkills.Remove(skill);
        }
        NotifySkillsChanged();
        return true;
    }

    //유물 Runtime이 X 스킬을 등록. 일반 보유 목록에 넣지 않고 다른 지급자는 덮어쓰지 않음
    public bool SetExclusiveSkill(SkillData skill, object source)
    {
        Initialize();
        if (skill == null || source == null || IsUsingSkill) return false;
        if (exclusiveSource != null && !ReferenceEquals(exclusiveSource, source)) return false;
        if (ownedSkills.Contains(skill)) return false;
        exclusiveSkill = skill;
        exclusiveSource = source;
        equippedSkills[ExclusiveSlot] = skill;
        NotifySkillsChanged();
        NotifyCooldown(ExclusiveSlot);
        return true;
    }

    //해당 Runtime이 지급한 X 스킬만 회수
    public bool ClearExclusiveSkill(object source)
    {
        Initialize();
        if (source == null || IsUsingSkill || !ReferenceEquals(exclusiveSource, source)) return false;
        exclusiveSkill = null;
        exclusiveSource = null;
        ClearSlot(ExclusiveSlot);
        NotifySkillsChanged();
        return true;
    }

    //일반 스킬의 ASDF 장착 조건 확인. X 수동 변경과 X 스킬의 ASDF 배치 거절
    public bool CanEquipSkill(int slotIndex, SkillData skill, out string reason)
    {
        Initialize();
        reason = null;
        if (!IsValidSlot(slotIndex)) reason = "잘못된 스킬 슬롯입니다.";
        else if (slotIndex == ExclusiveSlot) reason = "X 스킬은 검 유물로만 변경할 수 있습니다.";
        else if (skill == null) reason = "스킬이 없습니다.";
        else if (isChangingEquipment || IsUsingSkill) reason = "스킬 사용 또는 유물 변경 중입니다.";
        else if (skill == exclusiveSkill) reason = "X 전용 스킬은 ASDF에 장착할 수 없습니다.";
        else if (!ownedSkills.Contains(skill)) reason = "보유하지 않은 일반 스킬입니다.";
        else if (equippedSkills[slotIndex] != null && equippedSkills[slotIndex] != skill)
            reason = "이미 다른 스킬이 장착된 슬롯입니다.";
        else if (IsSkillEquipped(skill) && GetEquippedSlot(skill) != slotIndex)
            reason = "다른 슬롯에서 먼저 해제해야 합니다.";
        return reason == null;
    }

    //보유한 일반 스킬을 빈 ASDF 슬롯에 장착. 슬롯 이동은 기존 슬롯 해제 후 호출
    public bool EquipSkill(int slotIndex, SkillData skill)
    {
        if (!CanEquipSkill(slotIndex, skill, out string reason)) return false;
        if (equippedSkills[slotIndex] == skill) return true;
        equippedSkills[slotIndex] = skill;
        NotifySkillsChanged();
        NotifyCooldown(slotIndex);
        return true;
    }

    //ASDF 슬롯만 비우고 보유·쿨타임 유지. X 수동 해제는 거절
    public bool UnequipSkill(int slotIndex)
    {
        Initialize();
        if (!IsValidSlot(slotIndex) || slotIndex == ExclusiveSlot) return false;
        if (isChangingEquipment || IsUsingSkill || equippedSkills[slotIndex] == null) return false;
        ClearSlot(slotIndex);
        NotifySkillsChanged();
        return true;
    }

    //ASDF 슬롯만 비우고 보유·쿨타임 유지. X 수동 해제는 거절
    public bool UnequipSkill(int slotIndex, SkillData expectedSkill)
    {
        if (expectedSkill == null || GetEquippedSkill(slotIndex) != expectedSkill) return false;
        return UnequipSkill(slotIndex);
    }

    //슬롯을 비우고 쿨타임 표시 변경을 알림
    private void ClearSlot(int slotIndex)
    {
        equippedSkills[slotIndex] = null;
        NotifyCooldown(slotIndex);
    }

    //유물 변경 중 수동 스킬 장착을 막고 변경 이벤트를 모음
    internal void BeginEquipmentChange()
    {
        Initialize();
        isChangingEquipment = true;
    }

    //유물 변경 완료 후 최종 스킬 목록과 쿨타임 상태를 UI에 알림
    internal void EndEquipmentChange()
    {
        isChangingEquipment = false;
        bool changed = skillsChanged;
        skillsChanged = false;
        if (changed) OnSkillsChanged?.Invoke();
        for (int i = 0; i < SlotCount; i++)
        {
            if (!cooldownChanged[i]) continue;
            cooldownChanged[i] = false;
            NotifyCooldown(i);
        }
    }

    //해당 Runtime이 지급한 일반 스킬과 X 스킬을 모두 회수
    internal void RevokeSource(object source)
    {
        SkillData[] snapshot = ownedSkills.ToArray();
        foreach (SkillData skill in snapshot) RevokeSkill(skill, source);
        ClearExclusiveSkill(source);
    }

    //교체 실패 시 복구할 기존 슬롯 배치를 복사
    internal SkillData[] CaptureSlots()
    {
        Initialize();
        return (SkillData[])equippedSkills.Clone();
    }

    //기존 보유권 복구 후 ASDF 슬롯 배치를 되돌림
    internal void RestoreSlots(SkillData[] snapshot)
    {
        if (snapshot == null || snapshot.Length != SlotCount) return;
        for (int i = FirstNormalSlot; i < SlotCount; i++)
        {
            equippedSkills[i] = snapshot[i] != null && ownedSkills.Contains(snapshot[i]) ? snapshot[i] : null;
            NotifyCooldown(i);
        }
        equippedSkills[ExclusiveSlot] = exclusiveSkill;
        NotifySkillsChanged();
    }

    //일반 보유 스킬 또는 X/ASDF 장착 변경을 UI에 알림
    private void NotifySkillsChanged()
    {
        if (isChangingEquipment)
        {
            skillsChanged = true;
            return;
        }
        OnSkillsChanged?.Invoke();
    }

    //스킬 또는 해당 슬롯의 남은 쿨타임 조회. 해제·재장착해도 종료 시각 유지
    public float GetCooldownRemaining(SkillData skill)
    {
        if (skill == null || !cooldownEnds.TryGetValue(skill, out float endTime)) return 0f;
        return Mathf.Max(0f, endTime - Time.time);
    }

    //스킬 또는 해당 슬롯의 남은 쿨타임 조회. 해제·재장착해도 종료 시각 유지
    public float GetCooldownRemaining(int slotIndex) => GetCooldownRemaining(GetEquippedSkill(slotIndex));

    //해당 슬롯 스킬의 쿨타임이 남아 있는지 확인
    public bool IsOnCooldown(int slotIndex) => GetCooldownRemaining(slotIndex) > 0f;

    //슬롯 번호·남은 시간·전체 쿨타임을 UI에 전달
    private void NotifyCooldown(int slotIndex)
    {
        if (isChangingEquipment)
        {
            cooldownChanged[slotIndex] = true;
            return;
        }
        SkillData skill = GetEquippedSkill(slotIndex);
        OnSkillCooldownChanged?.Invoke(
            slotIndex, GetCooldownRemaining(skill), skill != null ? skill.cooldownTime : 0f);
    }

    //현재 슬롯 스킬의 쿨타임을 명시적으로 초기화
    public void ResetCooldown(int slotIndex)
    {
        SkillData skill = GetEquippedSkill(slotIndex);
        if (skill == null) return;
        cooldownEnds.Remove(skill);
        NotifyCooldown(slotIndex);
    }

    //X 입력을 전용 스킬 실행으로 연결
    public void ExecuteSkillX(Player p) { UseSkill(p, 0); }
    //A 입력을 스킬 실행으로 연결
    public void ExecuteSkillA(Player p) { UseSkill(p, 1); }
    //S 입력을 스킬 실행으로 연결
    public void ExecuteSkillS(Player p) { UseSkill(p, 2); }
    //D 입력을 스킬 실행으로 연결
    public void ExecuteSkillD(Player p) { UseSkill(p, 3); }
    //F 입력을 스킬 실행으로 연결
    public void ExecuteSkillF(Player p) { UseSkill(p, 4); }

    //보유·행동 상태·쿨타임·개별 사용 조건을 검사하고 스킬 실행
    public void UseSkill(Player p, int slotIndex)
    {
        Initialize();
        if (p == null || p != player || !IsValidSlot(slotIndex) || IsUsingSkill) return;
        if (isChangingEquipment || !p.ActionState.CanSkill() || IsOnCooldown(slotIndex)) return;
        SkillData skill = GetEquippedSkill(slotIndex);
        if (!HasSkill(skill) || !skill.CanUse(p)) return;
        StartCoroutine(SkillRoutine(p, skill, slotIndex));
    }

    //실제 스킬 실행 후 행동을 복구하고 쿨타임 시작. HUD 미연결이어도 실행 가능
    private IEnumerator SkillRoutine(Player p, SkillData skill, int slotIndex)
    {
        activeSkill = skill;
        p.ActionState.EnterSkill();
        try
        {
            OnSkillUsed?.Invoke(skill);
            yield return skill.ProcessSkill(p);
        }
        finally
        {
            if (p != null && p.ActionState.isSkillActive) p.ActionState.EnterNormal();
            activeSkill = null;
            cooldownEnds[skill] = Time.time + Mathf.Max(0f, skill.cooldownTime);
        }

        if (hudPanel != null) hudPanel.StartSkillCooldown(slotIndex, skill.cooldownTime);
        NotifyCooldown(slotIndex);
    }

    //폭주의 Q 스킬을 일반 슬롯과 별도로 등록
    public bool RegisterAwakeningSkill(ScorchedEarthSkillData skill)
    {
        if (skill == null || (awakeningSkill != null && awakeningSkill != skill))
        {
            return false;
        }
        awakeningSkill = skill;
        awakeningSkillAvailable = false;
        return true;
    }

    //등록된 Q 스킬을 해제하고 진행 중인 Q 판정 정리
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

    //각성 시작·종료에 따라 Q 스킬의 1회 사용권 설정
    public void SetAwakeningSkillAvailable(bool available)
    {
        awakeningSkillAvailable = awakeningSkill != null && available;
    }

    //Q 스킬의 피해를 확정해 실행하고 각성 종료
    public bool TryUseAwakeningSkill(Player p)
    {
        if (p == null || isChangingEquipment || activeSkill != null || awakeningSkill == null || !awakeningSkillAvailable) return false;
        if (awakeningSkillRoutine != null || !awakeningSkill.CanUse(p)) return false;

        ScorchedEarthSkillData skill = awakeningSkill;
        DamageInfo damageInfo = skill.PrepareDamage(p);
        awakeningSkillAvailable = false;
        awakeningSkillRoutine = StartCoroutine(AwakeningSkillRoutine(p, skill, damageInfo));

        p.Awakening.EndAwakening();
        return true;
    }

    //Q 스킬을 실행하고 행동 상태 복구
    private IEnumerator AwakeningSkillRoutine(Player p, ScorchedEarthSkillData skill, DamageInfo damageInfo)
    {
        p.ActionState.EnterSkill();
        OnSkillUsed?.Invoke(skill);
        yield return skill.ProcessPreparedSkill(p, damageInfo);
        if (p != null && p.ActionState.isSkillActive) p.ActionState.EnterNormal();
        awakeningSkillRoutine = null;
    }

}
