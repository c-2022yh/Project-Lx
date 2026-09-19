using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드래그하는 동안 커서를 따라다니는 유물 아이콘.
///
/// 실제 슬롯을 끌고 다니면 레이아웃이 흐트러지므로,
/// 캔버스 맨 위에 별도의 이미지 하나를 띄우고 원본 슬롯은 흐리게 처리한다.
/// </summary>
public class RelicDragLayer : MonoBehaviour
{
    [SerializeField] private RectTransform ghost;
    [SerializeField] private UnityEngine.UI.Image ghostImage;

    private RectTransform canvasRect;
    private Canvas canvas;

    public bool IsDragging { get; private set; }
    public RelicData DraggedRelic { get; private set; }

    /// <summary>드래그를 시작한 슬롯. 장착칸에서 끌어냈는지 판단할 때 쓴다.</summary>
    public RelicSlotView SourceSlot { get; private set; }

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvasRect = canvas.transform as RectTransform;

        if (ghost != null) ghost.gameObject.SetActive(false);
    }

    public void Begin(RelicSlotView source, RelicData relic, Vector2 screenPoint)
    {
        if (relic == null) return;

        IsDragging = true;
        DraggedRelic = relic;
        SourceSlot = source;

        if (ghostImage != null)
        {
            ghostImage.sprite = relic.Icon;
            ghostImage.enabled = relic.Icon != null;
        }

        if (ghost != null)
        {
            ghost.gameObject.SetActive(true);
            // 드래그 중인 아이콘이 다른 UI를 가리지 않게 맨 앞으로.
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
        DraggedRelic = null;
        SourceSlot = null;

        if (ghost != null) ghost.gameObject.SetActive(false);
    }
}
