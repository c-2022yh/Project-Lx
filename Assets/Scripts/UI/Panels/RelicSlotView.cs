using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>슬롯이 속한 영역. 드롭을 받았을 때 장착인지 해제인지 판단하는 데 쓴다.</summary>
public enum RelicSlotArea
{
    /// <summary>왼쪽 장착칸.</summary>
    Equip,

    /// <summary>오른쪽 보관함.</summary>
    Storage
}

/// <summary>
/// 유물 한 칸. 장착칸과 보관함 양쪽에서 같은 프리팹을 쓴다.
///
/// 이 스크립트는 "보여주기"와 "입력 받기"만 한다.
/// 무엇을 장착할지 같은 판단은 전부 host(RelicInventoryPanel)가 한다.
///
/// Button을 쓰지 않는 이유:
///   Button(Selectable)은 좌클릭만 이벤트로 주고, hover/press마다 색을 제 마음대로
///   덧칠해서 아래 SetVisualState가 정한 색과 싸운다. 우클릭·드래그도 받아야 하므로
///   EventSystem 인터페이스를 직접 구현한다.
/// </summary>
public class RelicSlotView : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("연결")]
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI costText;

    [Tooltip("마우스로 고른 유물 테두리 (노랑).")]
    [SerializeField] private Image selectionOutline;

    [Tooltip("키보드 화살표가 가리키는 칸 테두리 (하양).")]
    [SerializeField] private Image focusOutline;

    // 창 배경(0.08)과 충분히 구분되도록 밝힌 값.
    private static readonly Color FilledBg = new Color(0.28f, 0.28f, 0.34f, 1f);
    private static readonly Color EmptyBg = new Color(0.18f, 0.18f, 0.22f, 1f);
    private static readonly Color HoverBg = new Color(0.38f, 0.38f, 0.46f, 1f);

    /// <summary>드래그 중인 원본 칸은 흐리게 해서 "들려 있다"는 걸 보여준다.</summary>
    private const float DraggingIconAlpha = 0.25f;

    public RelicData Relic { get; private set; }

    public RelicSlotArea Area { get; private set; }

    /// <summary>
    /// 장착칸이면 그 칸이 받는 카테고리. 보관함 칸이면 유물 자신의 카테고리.
    /// 빈 장착칸도 어떤 계열인지 알아야 드롭을 받을 수 있어서 따로 들고 있다.
    /// </summary>
    public RelicCategory Category { get; private set; }

    /// <summary>키보드 이동용 격자 좌표.</summary>
    public int Row { get; private set; }
    public int Column { get; private set; }

    public bool IsEmpty => Relic == null;

    private IRelicSlotHost host;
    private bool isHovered;
    private bool isSelected;
    private bool isFocused;
    private bool isDraggingFrom;

    public void Bind(IRelicSlotHost slotHost, RelicData relic, RelicSlotArea area,
                     RelicCategory category, int row, int column)
    {
        host = slotHost;
        Relic = relic;
        Area = area;
        Category = category;
        Row = row;
        Column = column;

        isHovered = false;
        isDraggingFrom = false;

        bool hasRelic = relic != null;

        if (icon != null)
        {
            icon.sprite = hasRelic ? relic.Icon : null;
            icon.enabled = hasRelic && relic.Icon != null;
            icon.color = Color.white;
        }

        if (costText != null)
        {
            // 코스트는 신체 유물에만 의미가 있다. 검/보주는 칸이 하나뿐이라 표시하지 않는다.
            bool showCost = hasRelic && relic.Category == RelicCategory.Body;
            costText.enabled = showCost;
            if (showCost) costText.text = relic.Cost.ToString();
        }

        SetSelected(false);
        SetFocused(false);
        RefreshBackground();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectionOutline != null) selectionOutline.enabled = selected;
    }

    public void SetFocused(bool focused)
    {
        isFocused = focused;
        if (focusOutline != null) focusOutline.enabled = focused;
    }

    /// <summary>이 칸의 유물을 지금 끌고 다니는 중인지. 아이콘을 흐리게 만든다.</summary>
    public void SetDraggingFrom(bool dragging)
    {
        isDraggingFrom = dragging;

        if (icon == null) return;

        Color c = icon.color;
        c.a = dragging ? DraggingIconAlpha : 1f;
        icon.color = c;
    }

    private void RefreshBackground()
    {
        if (background == null) return;

        if (isHovered) background.color = HoverBg;
        else background.color = IsEmpty ? EmptyBg : FilledBg;
    }

    // ── 마우스 ─────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        RefreshBackground();

        if (host != null) host.OnSlotHoverEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        RefreshBackground();

        if (host != null) host.OnSlotHoverExit(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (host == null) return;

        // 드래그가 끝나는 순간에도 Click이 한 번 더 들어온다. 그건 무시한다.
        if (eventData.dragging) return;

        if (eventData.button == PointerEventData.InputButton.Right)
            host.OnSlotRightClick(this);
        else if (eventData.button == PointerEventData.InputButton.Left)
            host.OnSlotLeftClick(this);
    }

    // ── 드래그 ─────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (host == null) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (IsEmpty) return;

        host.OnSlotBeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (host == null) return;

        host.OnSlotDrag(this, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (host == null) return;

        host.OnSlotEndDrag(this, eventData);
    }

    /// <summary>
    /// 다른 칸에서 끌어온 유물을 이 칸 위에서 놓았을 때.
    /// 칸이 처리하지 않으면 부모의 RelicDropZone으로 올라간다.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (host == null) return;

        host.OnSlotDrop(this, eventData);
    }
}
