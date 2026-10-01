using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드래그하는 동안 커서를 따라다니는 스킬 아이콘.
/// 실제 칸을 끌고 다니면 레이아웃이 흐트러지므로 별도 이미지 하나를 띄운다.
/// </summary>
public class SkillDragLayer : MonoBehaviour
{
    [SerializeField] private RectTransform ghost;
    [SerializeField] private UnityEngine.UI.Image ghostImage;

    private RectTransform canvasRect;
    private Canvas canvas;

    public bool IsDragging { get; private set; }

    public SkillData DraggedSkill { get; private set; }

    /// <summary>드래그를 시작한 칸. 장착칸에서 끌어냈는지 판단할 때 쓴다.</summary>
    public SkillPanelSlot SourceSlot { get; private set; }

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvasRect = canvas.transform as RectTransform;

        if (ghost != null) ghost.gameObject.SetActive(false);
    }

    public void Begin(SkillPanelSlot source, SkillData skill, Vector2 screenPoint)
    {
        if (skill == null) return;

        IsDragging = true;
        DraggedSkill = skill;
        SourceSlot = source;

        if (ghostImage != null)
        {
            ghostImage.sprite = skill.icon;
            ghostImage.enabled = skill.icon != null;
        }

        if (ghost != null)
        {
            ghost.gameObject.SetActive(true);
            ghost.SetAsLastSibling();
        }

        Move(screenPoint);
    }

    public void Move(Vector2 screenPoint)
    {
        if (!IsDragging || ghost == null || canvasRect == null) return;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, cam, out Vector2 local))
            ghost.anchoredPosition = local;
    }

    public void End()
    {
        IsDragging = false;
        DraggedSkill = null;
        SourceSlot = null;

        if (ghost != null) ghost.gameObject.SetActive(false);
    }
}
