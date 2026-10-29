using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 이름 카드·HP바처럼 코드로 만드는 연출 UI의 공통 도구.
/// 캔버스, 이미지, 글자를 만들고 프로젝트 글꼴(제주명조)을 찾아 쓴다.
/// </summary>
public static class PresentationUI
{
    private static TMP_FontAsset font;

    public static readonly Color TextMain = new Color(0.92f, 0.93f, 0.92f, 1f);
    public static readonly Color TextDim = new Color(0.66f, 0.70f, 0.72f, 1f);

    public static Canvas NewCanvas(string name, int sortingOrder)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup group = go.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.alpha = 0f;
        return canvas;
    }

    public static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    public static Image NewImage(string name, Transform parent, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static TextMeshProUGUI NewText(string name, Transform parent, float size, Color color, TextAlignmentOptions align)
    {
        RectTransform rt = NewRect(name, parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset f = Font;
        if (f != null) t.font = f;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>부모를 꽉 채우게 한다.</summary>
    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public static TMP_FontAsset Font
    {
        get
        {
            if (font != null) return font;

            foreach (TMP_FontAsset f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (f != null && f.name.StartsWith("JejuMyeongjo")) return font = f;
            }

            TMP_FontAsset main = TMP_Settings.defaultFontAsset;
            if (main != null && !main.name.StartsWith("LiberationSans")) return font = main;

            TextMeshProUGUI any = Object.FindAnyObjectByType<TextMeshProUGUI>();
            if (any != null && any.font != null) return font = any.font;

            return font = main;
        }
    }
}
