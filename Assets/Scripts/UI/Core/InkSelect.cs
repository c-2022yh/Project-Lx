using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 10.31 UI 통일: "고른 상태" 표시. 메뉴·슬롯·조작법 항목이 모두 이걸 쓴다.
///   - 글씨가 밝아지고
///   - 글씨 아래에 먹 붓줄이 왼쪽부터 그어지고
///   - 글씨 뒤에 달빛이 은은하게 번진다.
/// 마우스를 올리면 그 항목이 골라진다 (키보드로 고른 것과 같은 모양).
/// </summary>
[DisallowMultipleComponent]
public class InkSelect : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
{
    private const float InkTime = 0.3f;

    private TextMeshProUGUI label;
    private Selectable selectable;
    private Image ink, glow;
    private Color idle, hot;
    private bool selected;
    private float clock;
    private bool useGlow;

    /// <summary>selectable에 붙인다. label 아래에 붓줄을 긋는다.</summary>
    public static InkSelect Attach(Selectable selectable, TextMeshProUGUI label, bool glow = true,
        Color? idleColor = null, Color? hotColor = null)
    {
        if (selectable == null || label == null) return null;
        InkSelect s = selectable.GetComponent<InkSelect>();
        if (s == null) s = selectable.gameObject.AddComponent<InkSelect>();
        s.Setup(selectable, label, glow, idleColor ?? UnifiedStyle.MenuIdle, hotColor ?? UnifiedStyle.MoonText);
        return s;
    }

    private void Setup(Selectable target, TextMeshProUGUI text, bool withGlow, Color idleColor, Color hotColor)
    {
        selectable = target;
        label = text;
        idle = idleColor;
        hot = hotColor;
        useGlow = withGlow;

        if (glow == null && useGlow)
        {
            glow = UnifiedStyle.NewImage("InkGlow", transform, UnifiedStyle.Glow, UnifiedStyle.GlowSprite());
            glow.transform.SetSiblingIndex(0);
            RectTransform g = glow.rectTransform;
            g.anchorMin = g.anchorMax = new Vector2(0f, 0.5f);
            g.pivot = new Vector2(0.5f, 0.5f);
        }

        if (ink == null)
        {
            ink = UnifiedStyle.NewImage("Ink", label.transform, new Color(0.88f, 0.90f, 0.97f, 0.8f), UnifiedStyle.InkSprite());
            ink.type = Image.Type.Filled;
            ink.fillMethod = Image.FillMethod.Horizontal;
            ink.fillOrigin = (int)Image.OriginHorizontal.Left;
            RectTransform r = ink.rectTransform;
            // textBounds는 글씨 피벗 기준이라, 붓줄도 피벗에 붙인다
            r.anchorMin = r.anchorMax = label.rectTransform.pivot;
            r.pivot = new Vector2(0f, 0.5f);
        }

        selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        Apply(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable != null && selectable.IsInteractable() && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        clock = 0f;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
    }

    private void OnEnable()
    {
        // 창을 다시 열었을 때 이전 상태가 남지 않게
        selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        clock = selected ? InkTime : 0f;
        Apply(true);
    }

    private void Update()
    {
        clock += Time.unscaledDeltaTime;
        Apply(false);
    }

    private void Apply(bool instant)
    {
        if (label == null) return;

        label.color = selected ? hot : idle;

        // 글씨 실제 크기에 맞춰 붓줄·빛 자리를 잡는다
        Bounds b = label.textBounds;
        float width = b.size.x > 1f ? b.size.x : label.preferredWidth;
        float left = b.size.x > 1f ? b.min.x : -label.rectTransform.rect.width * label.rectTransform.pivot.x;
        float bottom = b.size.y > 1f ? b.min.y : -label.fontSize * 0.5f;

        if (ink != null)
        {
            RectTransform r = ink.rectTransform;
            r.anchoredPosition = new Vector2(left - 4f, bottom - 6f);
            r.sizeDelta = new Vector2(width + 22f, Mathf.Max(7f, label.fontSize * 0.2f));
            float u = selected ? Mathf.Clamp01(instant ? 1f : clock / InkTime) : 0f;
            ink.fillAmount = 1f - (1f - u) * (1f - u);
            ink.enabled = selected;
        }

        if (glow != null)
        {
            RectTransform g = glow.rectTransform;
            Vector3 world = label.transform.TransformPoint(new Vector3(left + width * 0.3f, (b.min.y + b.max.y) * 0.5f, 0f));
            g.position = world;
            g.sizeDelta = new Vector2(width + 220f, label.fontSize * 2.6f);
            glow.enabled = selected;
        }
    }
}
