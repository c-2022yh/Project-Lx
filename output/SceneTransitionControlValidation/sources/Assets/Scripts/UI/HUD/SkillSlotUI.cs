using UnityEngine;
using UnityEngine.UI;

// 실제 장착과 쿨타임은 PlayerSkill이 관리하고 HUDPanel이 전달한다.
public class SkillSlotUI : MonoBehaviour
{
    [Header("Slot Identity")]
    [SerializeField] private SkillSlotType slotType = SkillSlotType.SlotA;

    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay; // Filled Radial 360

    [Header("Empty Slot")]
    [SerializeField] private Sprite emptySlotSprite; // 미장착 시 표시할 빈 프레임

    private SkillData currentSkill;
    private bool isOnCooldown;
    private bool isActive;

    public void Bind(Sprite icon, float cooldown)
    {
        if (iconImage != null)
        {
            // 스킬 아이콘이 없으면 빈 프레임을 표시한다.
            iconImage.sprite = icon != null ? icon : emptySlotSprite;
            iconImage.enabled = iconImage.sprite != null;
        }
        isActive = true;
        SetCooldown(0f, cooldown);
    }

    public void BindSkill(SkillData skill)
    {
        if (skill == null) { Clear(); return; }
        currentSkill = skill;
        Bind(skill.icon, skill.cooldownTime);
    }

    public void Clear()
    {
        currentSkill = null;
        isActive = false;
        SetCooldown(0f, 0f);
        if (iconImage != null)
        {
            // 스킬을 해제해도 슬롯 테두리는 유지한다.
            iconImage.sprite = emptySlotSprite;
            iconImage.enabled = emptySlotSprite != null;
        }
    }

    // 독립 타이머 없이 실제 남은 시간을 표시한다.
    public void SetCooldown(float remaining, float duration)
    {
        isOnCooldown = isActive && remaining > 0f && duration > 0f;
        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = isOnCooldown
                ? Mathf.Clamp01(remaining / duration) : 0f;
    }

    public SkillData CurrentSkill => currentSkill;
    public SkillSlotType SlotType => slotType;
    public bool IsActive => isActive;
    public bool IsOnCooldown => isOnCooldown;
}

public enum SkillSlotType
{
    SlotX,
    SlotA,
    SlotS,
    SlotD,
    SlotF
}
