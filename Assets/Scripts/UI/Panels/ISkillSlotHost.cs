using UnityEngine.EventSystems;

/// <summary>
/// 스킬 칸이 입력을 넘길 곳. 실제 판단은 전부 SkillPanel이 한다.
/// 칸은 "보여주기"와 "입력 받기"만 한다.
/// </summary>
public interface ISkillSlotHost
{
    void OnSkillSlotHoverEnter(SkillPanelSlot slot);

    void OnSkillSlotHoverExit(SkillPanelSlot slot);

    void OnSkillSlotLeftClick(SkillPanelSlot slot);

    void OnSkillSlotRightClick(SkillPanelSlot slot);

    void OnSkillSlotBeginDrag(SkillPanelSlot slot, PointerEventData eventData);

    void OnSkillSlotDrag(SkillPanelSlot slot, PointerEventData eventData);

    void OnSkillSlotEndDrag(SkillPanelSlot slot, PointerEventData eventData);

    void OnSkillSlotDrop(SkillPanelSlot target, PointerEventData eventData);
}
