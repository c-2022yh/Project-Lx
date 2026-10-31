using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 지금 고른(또는 마우스를 올린) UI 항목에 「 」 모양 모서리 테두리를 그려준다. (10.27, 창살 테두리와 같은 결)
///   왼쪽 위에 ┌, 오른쪽 아래에 ┘.
///
/// 어느 씬에도 붙이지 않는다. 게임이 시작되면 스스로 생기고, 모든 버튼·슬라이더·드롭다운에 자동으로 적용된다.
///   - 키보드로 움직이면 EventSystem에서 선택된 항목을 따라간다.
///   - 마우스를 움직이면 마우스가 올라간 항목을 따라간다.
///   - 글자만 있는 버튼(타이틀 메뉴 등)은 버튼 칸이 아니라 글자 크기에 맞춘다.
/// 테두리를 빼고 싶은 항목에는 NoSelectionBrackets 컴포넌트를 붙이면 된다.
/// </summary>
public class SelectionBrackets : MonoBehaviour
{
    private const string SpritePath = "UIFrame/Bracket_Corner";

    private static readonly Color BracketColor = new Color(0.86f, 0.89f, 0.97f, 0.95f);
    private const float Gap = 12f;          // 항목 가장자리에서 바깥으로 띄우는 거리
    private const float SlideFrom = 10f;    // 나타날 때 이만큼 바깥에서 안으로 들어온다
    private const float AppearSeconds = 0.14f;
    private const float MinArm = 12f, MaxArm = 24f;

    private static SelectionBrackets instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("[SelectionBrackets]");
        instance = go.AddComponent<SelectionBrackets>();
        DontDestroyOnLoad(go);
    }

    private Sprite sprite;
    private RectTransform topLeft, bottomRight;
    private Image topLeftImage, bottomRightImage;
    private RectTransform target;
    private float shownAt;
    private bool mouseMode;
    private Vector2 lastMouse;
    private readonly List<RaycastResult> hits = new List<RaycastResult>();

    private void Awake()
    {
        sprite = Resources.Load<Sprite>(SpritePath);
        if (sprite == null) Debug.LogWarning("[SelectionBrackets] 그림이 없습니다: Resources/" + SpritePath);
    }

    private void LateUpdate()
    {
        if (sprite == null) return;

        UpdateInputMode();
        RectTransform next = FindTarget();

        if (next != target) Attach(next);
        if (target == null) return;

        Layout();
    }

    // ── 대상 고르기 ─────────────────────────

    private void UpdateInputMode()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 p = mouse.position.ReadValue();
            if ((p - lastMouse).sqrMagnitude > 4f || mouse.leftButton.wasPressedThisFrame) mouseMode = true;
            lastMouse = p;
        }

        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ||
                           kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame ||
                           kb.tabKey.wasPressedThisFrame))
            mouseMode = false;

        Gamepad pad = Gamepad.current;
        if (pad != null && pad.dpad.ReadValue().sqrMagnitude > 0.1f) mouseMode = false;
    }

    private RectTransform FindTarget()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return null;

        if (mouseMode && Mouse.current != null)
        {
            Selectable hovered = HoveredSelectable(es, Mouse.current.position.ReadValue());
            if (hovered != null) return Accept(hovered) ? (RectTransform)hovered.transform : null;
        }

        GameObject selected = es.currentSelectedGameObject;
        if (selected == null) return null;

        Selectable s = selected.GetComponent<Selectable>();
        return s != null && Accept(s) ? (RectTransform)s.transform : null;
    }

    private Selectable HoveredSelectable(EventSystem es, Vector2 screen)
    {
        PointerEventData data = new PointerEventData(es) { position = screen };
        hits.Clear();
        es.RaycastAll(data, hits);
        foreach (RaycastResult hit in hits)
        {
            if (hit.gameObject == null) continue;
            Selectable s = hit.gameObject.GetComponentInParent<Selectable>();
            if (s != null) return s;
            return null; // 맨 위에 맞은 것이 버튼이 아니면 그 아래는 가려진 것
        }
        return null;
    }

    private static bool Accept(Selectable s)
    {
        if (s == null || !s.isActiveAndEnabled || !s.IsInteractable()) return false;
        if (s is Scrollbar) return false;
        if (s.GetComponent<NoSelectionBrackets>() != null) return false;
        return s.transform is RectTransform;
    }

    // ── 붙이기 / 배치 ───────────────────────

    private void Attach(RectTransform next)
    {
        target = next;
        EnsureImages();

        if (target == null)
        {
            Hide();
            return;
        }

        topLeft.SetParent(target, false);
        bottomRight.SetParent(target, false);
        topLeft.SetAsLastSibling();
        bottomRight.SetAsLastSibling();
        topLeft.gameObject.SetActive(true);
        bottomRight.gameObject.SetActive(true);
        shownAt = Time.unscaledTime;
    }

    private void Hide()
    {
        if (topLeft == null) return;
        // 대상이 사라져도 같이 지워지지 않게 내 쪽으로 돌려놓는다.
        topLeft.SetParent(transform, false);
        bottomRight.SetParent(transform, false);
        topLeft.gameObject.SetActive(false);
        bottomRight.gameObject.SetActive(false);
    }

    private void EnsureImages()
    {
        if (topLeft != null && bottomRight != null) return;

        topLeft = NewCorner("Bracket_TL", out topLeftImage, 0f);
        bottomRight = NewCorner("Bracket_BR", out bottomRightImage, 180f);
    }

    private RectTransform NewCorner(string name, out Image image, float rotation)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(transform, false);
        go.GetComponent<LayoutElement>().ignoreLayout = true;

        image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = BracketColor;
        image.raycastTarget = false;

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 1f);   // 회전 전 기준: 왼쪽 위 모서리
        rt.localRotation = Quaternion.Euler(0f, 0f, rotation);
        go.SetActive(false);
        return rt;
    }

    private void Layout()
    {
        if (topLeft == null || topLeft.parent != target)
        {
            // 대상이 사라졌거나 테두리가 같이 지워졌다
            topLeft = bottomRight = null;
            target = null;
            return;
        }

        Rect r = ContentRect(target);
        float arm = Mathf.Clamp(Mathf.Min(r.width, r.height) * 0.4f, MinArm, MaxArm);

        float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / AppearSeconds);
        float ease = 1f - (1f - t) * (1f - t);
        float gap = Gap + SlideFrom * (1f - ease);
        Color c = BracketColor;
        c.a *= ease;
        topLeftImage.color = c;
        bottomRightImage.color = c;

        Vector2 center = target.rect.center;
        topLeft.sizeDelta = new Vector2(arm, arm);
        bottomRight.sizeDelta = new Vector2(arm, arm);
        topLeft.anchoredPosition = new Vector2(r.xMin - gap, r.yMax + gap) - center;
        // 180도 돌린 그림이라 피벗(왼쪽 위)이 오른쪽 아래 모서리로 간다
        bottomRight.anchoredPosition = new Vector2(r.xMax + gap, r.yMin - gap) - center;
    }

    /// <summary>테두리를 둘 영역 (대상의 로컬 좌표). 글자만 있는 버튼은 실제 글자 크기.</summary>
    private static Rect ContentRect(RectTransform rt)
    {
        Selectable s = rt.GetComponent<Selectable>();
        if (s != null && s.targetGraphic is TMP_Text text && text.textInfo != null && text.textInfo.characterCount > 0)
        {
            Bounds b = text.textBounds;
            Vector3 min = rt.InverseTransformPoint(text.transform.TransformPoint(b.min));
            Vector3 max = rt.InverseTransformPoint(text.transform.TransformPoint(b.max));
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y),
                                   Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }
        return rt.rect;
    }
}
