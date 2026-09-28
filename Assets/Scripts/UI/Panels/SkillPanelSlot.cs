using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>스킬 칸이 속한 영역.</summary>
public enum SkillSlotArea
{
    /// <summary>위쪽 장착칸. X / A / S / D / F 다섯 개.</summary>
    Equip,

    /// <summary>아래쪽 보유 스킬 목록.</summary>
    Storage
}

/// <summary>
/// 스킬 한 칸. 장착칸과 보유 목록 양쪽에서 같은 프리팹을 쓴다.
///
/// 유물 칸(RelicSlotView)과 같은 구조다. Button을 쓰지 않는 이유도 같다.
/// Button은 좌클릭만 이벤트로 주고 hover/press마다 색을 덧칠해서
/// 아래 RefreshBackground가 정한 색과 싸운다.
///
/// X칸은 잠긴 칸이다. PlayerSkill이 X 슬롯의 수동 변경과 해제를 거절하므로
/// 여기서도 드래그를 시작하지 않고 드롭도 받지 않는다.
/// </summary>
public class SkillPanelSlot : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("연결")]
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private Image selectionOutline;

    [Tooltip("장착칸 위에 붙는 키 이름(X, A, S, D, F). 보유 목록 칸에서는 꺼둔다.")]
    [SerializeField] private TextMeshProUGUI keyText;

    private static readonly Color FilledBg = new Color(0.28f, 0.28f, 0.34f, 1f);
    private static readonly Color EmptyBg = new Color(0.18f, 0.18f, 0.22f, 1f);
    private static readonly Color HoverBg = new Color(0.38f, 0.38f, 0.46f, 1f);

    /// <summary>잠긴 칸(X)은 손댈 수 없다는 게 보이도록 푸른 기를 준다.</summary>
    private static readonly Color LockedBg = new Color(0.20f, 0.22f, 0.30f, 1f);

    private static readonly Color KeyNormal = new Color(0.92f, 0.92f, 0.94f, 1f);
    private static readonly Color KeyLocked = new Color(0.55f, 0.60f, 0.75f, 1f);

    /// <summary>드래그 중인 원본 칸은 흐리게 해서 "들려 있다"는 걸 보여준다.</summary>
    private const float DraggingIconAlpha = 0.25f;

    public SkillData Skill { get; private set; }

    public SkillSlotArea Area { get; private set; }

    /// <summary>장착칸이면 PlayerSkill의 슬롯 번호. 보유 목록이면 -1.</summary>
    public int SlotIndex { get; private set; }

    /// <summary>X칸처럼 플레이어가 바꿀 수 없는 칸.</summary>
    public bool IsLocked { get; private set; }

    public bool IsEmpty => Skill == null;

    private ISkillSlotHost host;
    private bool isHovered;

    public void Bind(ISkillSlotHost slotHost, SkillData skill, SkillSlotArea area,
                     int slotIndex, string keyLabel, bool locked)
    {
        host = slotHost;
        Skill = skill;
        Area = area;
        SlotIndex = slotIndex;
        IsLocked = locked;

        isHovered = false;

        bool hasSkill = skill != null;

        if (icon != null)
        {
            icon.sprite = hasSkill ? skill.icon : null;
            icon.enabled = hasSkill && skill.icon != null;
            icon.color = Color.white;
        }

        if (keyText != null)
        {
            bool showKey = !string.IsNullOrEmpty(keyLabel);
            keyText.enabled = showKey;

            if (showKey)
            {
                keyText.text = keyLabel;
                keyText.color = locked ? KeyLocked : KeyNormal;
            }
        }

        SetSelected(false);
        RefreshBackground();
    }

    public void SetSelected(bool selected)
    {
        if (selectionOutline != null) selectionOutline.enabled = selected;
    }

    public void SetDraggingFrom(bool dragging)
    {
        if (icon == null) return;

        Color c = icon.color;
        c.a = dragging ? DraggingIconAlpha : 1f;
        icon.color = c;
    }

    private void RefreshBackground()
    {
        if (background == null) return;

        if (IsLocked) background.color = LockedBg;
        else if (isHovered) background.color = HoverBg;
        else background.color = IsEmpty ? EmptyBg : FilledBg;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        RefreshBackground();

        if (host != null) host.OnSkillSlotHoverEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        RefreshBackground();

        if (host != null) host.OnSkillSlotHoverExit(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (host == null) return;

        // 드래그가 끝나는 순간에도 Click이 한 번 더 들어온다. 그건 무시한다.
        if (eventData.dragging) return;

        if (eventData.button == PointerEventData.InputButton.Right)
            host.OnSkillSlotRightClick(this);
        else if (eventData.button == PointerEventData.InputButton.Left)
            host.OnSkillSlotLeftClick(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (host == null) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (IsEmpty || IsLocked) return;

        host.OnSkillSlotBeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (host == null) return;

        host.OnSkillSlotDrag(this, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (host == null) return;

        host.OnSkillSlotEndDrag(this, eventData);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (host == null) return;

        host.OnSkillSlotDrop(this, eventData);
    }
}
