using UnityEngine.EventSystems;

/// <summary>
/// 유물 칸이 입력을 넘겨줄 상대. 실제로는 RelicInventoryPanel이 구현한다.
///
/// 칸이 패널을 직접 알지 않게 해서, 칸은 "보여주고 입력 받기"만 하고
/// 장착 규칙 판단은 전부 패널 한 곳에 모이게 한다.
/// </summary>
public interface IRelicSlotHost
{
    void OnSlotHoverEnter(RelicSlotView slot);
    void OnSlotHoverExit(RelicSlotView slot);

    /// <summary>좌클릭: 선택.</summary>
    void OnSlotLeftClick(RelicSlotView slot);

    /// <summary>우클릭: 장착칸이면 해제, 보관함이면 알맞은 칸에 자동 장착.</summary>
    void OnSlotRightClick(RelicSlotView slot);

    void OnSlotBeginDrag(RelicSlotView slot, PointerEventData eventData);
    void OnSlotDrag(RelicSlotView slot, PointerEventData eventData);
    void OnSlotEndDrag(RelicSlotView slot, PointerEventData eventData);

    /// <summary>이 칸 위에 유물을 놓았을 때.</summary>
    void OnSlotDrop(RelicSlotView target, PointerEventData eventData);

    /// <summary>칸이 아니라 빈 영역(보관함 여백, 장착 줄의 빈 공간)에 놓았을 때.</summary>
    void OnZoneDrop(RelicDropZone zone, PointerEventData eventData);
}
