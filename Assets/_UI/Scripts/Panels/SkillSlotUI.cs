using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;


public class SkillSlotUI : MonoBehaviour
{
    [Header("Slot Identity")]
    [SerializeField] private SkillSlotType slotType = SkillSlotType.SlotA;

    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay;     // Filled Radial 360

    [Header("Settings — 일단 더미")]
    [SerializeField] private float cooldownDuration = 3f;
    [SerializeField] private Sprite dummyIcon;
    [SerializeField] private bool startActive = false;

    [Header("Current Skill Data")]
    [SerializeField] private SkillData currentSkill; // 현재 바인딩된 스킬

    private float cooldownRemaining;
    private bool isOnCooldown;
    private bool isActive;

    // [PLAYER_HOOK] 팀원 PlayerSkill에서 스킬 발동 시 이 이벤트로 알림
    public static event Action<SkillSlotType> OnSkillTriggered;

    void Start()
    {
        if (currentSkill != null) BindSkill(currentSkill);
        else if (startActive) Bind(dummyIcon, cooldownDuration);
        else Clear();
        
    }

    void Update()
    {
        if (!isActive) return;

        // 쿨타임 진행
        if (isOnCooldown)
        {
            cooldownRemaining -= Time.deltaTime;
            if (cooldownOverlay != null)
                cooldownOverlay.fillAmount = cooldownRemaining / cooldownDuration;
            if (cooldownRemaining <= 0f)
            {
                isOnCooldown = false;
                if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
            }
        }

    }

    /// <summary>슬롯에 스킬 끼우기 (활성화)</summary>
    public void Bind(Sprite icon, float cooldown)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = (icon != null);
        }
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
        cooldownDuration = cooldown;
        isActive = true;
        isOnCooldown = false;
    }
    /// <summary>SkillUIData로 슬롯 바인딩 (정식 방법)</summary>
    public void BindSkill(SkillData skill)
    {
        if (skill == null) { Clear(); return; }
        currentSkill = skill;
        Bind(skill.icon, skill.cooldownTime);
    }

    /// <summary>현재 바인딩된 스킬 (없으면 null)</summary>
    public SkillData CurrentSkill => currentSkill;
    /// <summary>슬롯 비우기 (비활성화)</summary>
    public void Clear()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
        isActive = false;
        isOnCooldown = false;
    }

    public void OnPointerClick(PointerEventData eventData) => TryTriggerSkill();

    /// <summary>외부에서 쿨타임 시작 (UIManager 등에서 호출)</summary>
    public void StartCooldown()
    {
        if (!isActive) return;
        cooldownRemaining = cooldownDuration;
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 1f;
        isOnCooldown = true;
    }

    //쿨타임을 받는 오버로드
    public void StartCooldown(float cooldownTime)
    {
        if (!isActive) return;

        cooldownDuration = cooldownTime;
        cooldownRemaining = cooldownTime;

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = 1f;

        isOnCooldown = true;
    }

    private void TryTriggerSkill()
    {
        if (!isActive || isOnCooldown) return;

        // [PLAYER_HOOK] 기력 비용 체크 자리
        // if (!UIManager.Instance.TryUseSoul(soulCost)) return;

        OnSkillTriggered?.Invoke(slotType);
        StartCooldown(cooldownDuration);

        Debug.Log($"[SkillSlot] {slotType} 발동! 쿨타임 {cooldownDuration}초");
    }

    public SkillSlotType SlotType => slotType;
    public bool IsActive => isActive;
    public bool IsOnCooldown => isOnCooldown;
}