using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 유물 위에 마우스를 올리면 뜨는 상세 툴팁.
/// 커서를 따라다니고, 화면 밖으로 나가지 않게 가장자리에서 방향을 뒤집는다.
///
/// 크기는 내용에 맞춰 자동으로 늘어난다 (VerticalLayoutGroup + ContentSizeFitter).
/// </summary>
public class RelicTooltip : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private UnityEngine.UI.Image iconImage;
    [SerializeField] private TextMeshProUGUI bodyText;

    [Header("동작")]
    [Tooltip("커서에서 얼마나 떨어뜨릴지.")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(20f, -20f);

    private RectTransform canvasRect;
    private Canvas canvas;
    private bool isShowing;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvasRect = canvas.transform as RectTransform;

        HideImmediate();
    }

    public void Show(RelicData relic)
    {
        if (relic == null)
        {
            Hide();
            return;
        }

        if (titleText != null) titleText.text = relic.RelicName;

        if (subtitleText != null)
        {
            // 코스트는 신체 유물에만 의미가 있다.
            subtitleText.text = relic.Category == RelicCategory.Body
                ? $"{RelicEquipRules.LabelOf(relic.Category)} 계열   ·   코스트 {relic.Cost}"
                : $"{RelicEquipRules.LabelOf(relic.Category)} 계열";
        }

        if (iconImage != null)
        {
            iconImage.sprite = relic.Icon;
            iconImage.enabled = relic.Icon != null;
        }

        if (bodyText != null) bodyText.text = relic.Description;

        if (panel == null)
        {
            Debug.LogWarning("[RelicTooltip] panel이 연결되지 않았습니다. Tools/UI > Build Relic Inventory 를 다시 실행해주세요.", this);
            return;
        }

        isShowing = true;
        panel.gameObject.SetActive(true);

        // 첫 프레임부터 올바른 자리에 뜨도록 즉시 배치한다.
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        Follow();
    }

    public void Hide()
    {
        isShowing = false;
        if (panel != null) panel.gameObject.SetActive(false);
    }

    private void HideImmediate()
    {
        isShowing = false;
        if (panel != null) panel.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!isShowing) return;
        Follow();
    }

    /// <summary>커서를 따라가되 화면 밖으로 나가면 반대쪽으로 붙인다.</summary>
    private void Follow()
    {
        if (panel == null || canvasRect == null) return;

        Vector2 screenPoint = GetPointerPosition();

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, cam, out Vector2 local))
            return;

        Vector2 size = panel.rect.size;
        Vector2 canvasSize = canvasRect.rect.size;

        // 기본은 커서 오른쪽 아래. pivot이 (0,1)이라 그 방향으로 펼쳐진다.
        Vector2 pos = local + cursorOffset;

        float halfW = canvasSize.x * 0.5f;
        float halfH = canvasSize.y * 0.5f;

        // 오른쪽으로 넘치면 커서 왼쪽에 띄운다.
        if (pos.x + size.x > halfW)
            pos.x = local.x - cursorOffset.x - size.x;

        // 아래로 넘치면 커서 위쪽에 띄운다.
        if (pos.y - size.y < -halfH)
            pos.y = local.y - cursorOffset.y + size.y;

        // 그래도 넘치면 가장자리에 붙인다.
        pos.x = Mathf.Clamp(pos.x, -halfW, halfW - size.x);
        pos.y = Mathf.Clamp(pos.y, -halfH + size.y, halfH);

        panel.anchoredPosition = pos;
    }

    /// <summary>
    /// Input System만 켜진 프로젝트에서는 Input.mousePosition이 예외를 던진다.
    /// 리플렉션 없이 안전하게 읽기 위해 Pointer.current를 쓴다.
    /// </summary>
    private static Vector2 GetPointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.Pointer pointer = UnityEngine.InputSystem.Pointer.current;
        if (pointer != null) return pointer.position.ReadValue();
        return Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
}
