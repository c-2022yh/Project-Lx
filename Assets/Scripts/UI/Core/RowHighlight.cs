using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 10.31 UI 통일: 설정 줄 하나. 마우스를 올리거나 그 줄의 칸(드롭다운·슬라이더 등)을 고르면
/// 왼쪽에 달빛 선이 서고, 달빛 띠가 옅게 깔리고, 이름이 밝아진다.
/// </summary>
[DisallowMultipleComponent]
public class RowHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image edge, band;
    private TextMeshProUGUI label;
    private bool hover;
    private float amount;

    public static RowHighlight Attach(RectTransform row, TextMeshProUGUI label)
    {
        if (row == null) return null;
        RowHighlight h = row.GetComponent<RowHighlight>();
        if (h == null) h = row.gameObject.AddComponent<RowHighlight>();
        h.label = label;

        if (h.band == null)
        {
            h.band = UnifiedStyle.NewImage("RowBand", row, new Color(0.63f, 0.71f, 1f, 0f), UnifiedStyle.BandSprite());
            UnifiedStyle.Stretch(h.band.rectTransform, -10f, 2f, 0f, 2f);
            h.band.raycastTarget = true;   // 줄의 빈 곳에 마우스를 올려도 알 수 있게 (맨 뒤라 칸은 그대로 눌린다)
            h.band.transform.SetSiblingIndex(0);

            h.edge = UnifiedStyle.NewImage("RowEdge", row, new Color(0.84f, 0.87f, 0.95f, 0f));
            RectTransform e = h.edge.rectTransform;
            e.anchorMin = new Vector2(0f, 0f);
            e.anchorMax = new Vector2(0f, 1f);
            e.pivot = new Vector2(0f, 0.5f);
            e.offsetMin = new Vector2(-10f, 6f);
            e.offsetMax = new Vector2(-8f, -6f);
            h.edge.transform.SetSiblingIndex(1);
        }
        return h;
    }

    public void OnPointerEnter(PointerEventData eventData) => hover = true;
    public void OnPointerExit(PointerEventData eventData) => hover = false;

    private void OnDisable()
    {
        hover = false;
        amount = 0f;
        Apply();
    }

    private void Update()
    {
        GameObject sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        bool on = hover || (sel != null && sel.transform.IsChildOf(transform));
        amount = Mathf.MoveTowards(amount, on ? 1f : 0f, Time.unscaledDeltaTime / 0.15f);
        Apply();
    }

    private void Apply()
    {
        if (band != null) band.color = new Color(0.63f, 0.71f, 1f, 0.12f * amount);
        if (edge != null) edge.color = new Color(0.84f, 0.87f, 0.95f, 0.75f * amount);
        if (label != null) label.color = Color.Lerp(new Color(0.73f, 0.74f, 0.78f, 1f), UnifiedStyle.MoonText, amount);
    }
}
