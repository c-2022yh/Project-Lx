#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 에디터 UI 빌더들이 공유하는 도구 모음.
/// InventoryUIBuilder의 보조 함수들과 같은 규칙을 쓰되, 여러 빌더가 재사용할 수 있게 뺐다.
/// (InventoryUIBuilder 자체는 건드리지 않았다.)
/// </summary>
public static class UIBuildKit
{
    public const string KoreanFontPath = "Assets/TextMesh Pro/Fonts/TerrarumSansBitmap SDF.asset";

    private static TMP_FontAsset koreanFont;

    public static TMP_FontAsset KoreanFont
    {
        get
        {
            if (koreanFont == null)
                koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            return koreanFont;
        }
    }

    public static bool EnsureKoreanFont()
    {
        if (KoreanFont != null) return true;

        EditorUtility.DisplayDialog(
            "한글 폰트 없음",
            "폰트 에셋을 찾을 수 없습니다:\n" + KoreanFontPath + "\n\n" +
            "경로가 바뀌었다면 UIBuildKit.KoreanFontPath를 수정해주세요.",
            "확인");

        return false;
    }

    // ── 색 (시안의 어두운 톤에 맞춤) ──────────
    public static readonly Color PanelBg = new Color(0.08f, 0.08f, 0.09f, 0.96f);
    public static readonly Color SectionBg = new Color(0.13f, 0.13f, 0.15f, 1f);
    public static readonly Color SlotBg = new Color(0.17f, 0.17f, 0.19f, 1f);
    public static readonly Color ButtonBg = new Color(0.22f, 0.22f, 0.25f, 1f);
    public static readonly Color AccentText = new Color(0.92f, 0.92f, 0.94f, 1f);
    public static readonly Color MutedText = new Color(0.62f, 0.62f, 0.66f, 1f);
    public static readonly Color Divider = new Color(1f, 1f, 1f, 0.12f);

    // ── 빌트인 스프라이트 ───────────────────
    public static Sprite Builtin(string path) =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>(path);

    public static DefaultControls.Resources UIResources()
    {
        return new DefaultControls.Resources
        {
            standard = Builtin("UI/Skin/UISprite.psd"),
            background = Builtin("UI/Skin/Background.psd"),
            inputField = Builtin("UI/Skin/InputFieldBackground.psd"),
            knob = Builtin("UI/Skin/Knob.psd"),
            checkmark = Builtin("UI/Skin/Checkmark.psd"),
            dropdown = Builtin("UI/Skin/DropdownArrow.psd"),
            mask = Builtin("UI/Skin/UIMask.psd")
        };
    }

    public static TMP_DefaultControls.Resources TMPResources()
    {
        return new TMP_DefaultControls.Resources
        {
            standard = Builtin("UI/Skin/UISprite.psd"),
            background = Builtin("UI/Skin/Background.psd"),
            inputField = Builtin("UI/Skin/InputFieldBackground.psd"),
            knob = Builtin("UI/Skin/Knob.psd"),
            checkmark = Builtin("UI/Skin/Checkmark.psd"),
            dropdown = Builtin("UI/Skin/DropdownArrow.psd"),
            mask = Builtin("UI/Skin/UIMask.psd")
        };
    }

    // ── 기본 생성 ──────────────────────────
    public enum Anchor { TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight, Center, MiddleLeft, MiddleRight }

    public static GameObject Obj(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static GameObject Img(string name, Transform parent, Color color, bool raycastTarget = false)
    {
        GameObject go = Obj(name, parent);
        UnityEngine.UI.Image img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = color;
        img.raycastTarget = raycastTarget;
        return go;
    }

    public static GameObject Text(string name, Transform parent, string text, float size,
                                  FontStyles style = FontStyles.Normal,
                                  TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft,
                                  Color? color = null)
    {
        GameObject go = Obj(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();

        if (KoreanFont != null) tmp.font = KoreanFont;

        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = color ?? AccentText;
        tmp.raycastTarget = false;
        return go;
    }

    public static GameObject Button(string name, Transform parent, string label, float fontSize = 22,
                                    Color? bgColor = null)
    {
        GameObject go = Obj(name, parent);
        UnityEngine.UI.Image bg = go.AddComponent<UnityEngine.UI.Image>();
        bg.color = bgColor ?? ButtonBg;
        bg.raycastTarget = true;

        UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = bg;

        GameObject text = Text("Label", go.transform, label, fontSize, FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(text, 0, 0, 0, 0);

        return go;
    }

    /// <summary>UnityEngine.UI.Slider 하나. 0~100 정수.</summary>
    public static Slider Slider(string name, Transform parent)
    {
        GameObject go = DefaultControls.CreateSlider(UIResources());
        go.name = name;
        go.transform.SetParent(parent, false);

        Slider slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true;

        return slider;
    }

    /// <summary>TMP_Dropdown 하나. 한글 폰트를 라벨/아이템에 지정한다.</summary>
    public static TMP_Dropdown Dropdown(string name, Transform parent)
    {
        GameObject go = TMP_DefaultControls.CreateDropdown(TMPResources());
        go.name = name;
        go.transform.SetParent(parent, false);

        TMP_Dropdown dropdown = go.GetComponent<TMP_Dropdown>();

        if (KoreanFont != null)
        {
            foreach (TextMeshProUGUI tmp in go.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                tmp.font = KoreanFont;
                tmp.fontSize = 20;
                tmp.color = AccentText;
            }
        }

        return dropdown;
    }

    // ── RectTransform 배치 ──────────────────

    public static void Stretch(GameObject go, float left, float top, float right, float bottom)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    /// <summary>위쪽 가장자리에 붙여 좌우로 늘림.</summary>
    public static void StretchTop(GameObject go, float left, float topOffset, float right, float height)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2((left - right) / 2f, -topOffset);
        rt.sizeDelta = new Vector2(-(left + right), height);
    }

    /// <summary>아래쪽 가장자리에 붙여 좌우로 늘림.</summary>
    public static void StretchBottom(GameObject go, float left, float bottomOffset, float right, float height)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2((left - right) / 2f, bottomOffset);
        rt.sizeDelta = new Vector2(-(left + right), height);
    }

    /// <summary>왼쪽 가장자리에 붙여 위아래로 늘림.</summary>
    public static void StretchLeft(GameObject go, float leftOffset, float top, float width, float bottom)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(leftOffset, (bottom - top) / 2f);
        rt.sizeDelta = new Vector2(width, -(top + bottom));
    }

    public static void Place(GameObject go, Anchor anchor, float x, float y, float w, float h)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        Vector2 a = anchor switch
        {
            Anchor.TopLeft => new Vector2(0, 1),
            Anchor.TopCenter => new Vector2(0.5f, 1),
            Anchor.TopRight => new Vector2(1, 1),
            Anchor.BottomLeft => new Vector2(0, 0),
            Anchor.BottomCenter => new Vector2(0.5f, 0),
            Anchor.BottomRight => new Vector2(1, 0),
            Anchor.MiddleLeft => new Vector2(0, 0.5f),
            Anchor.MiddleRight => new Vector2(1, 0.5f),
            _ => new Vector2(0.5f, 0.5f)
        };

        rt.anchorMin = a;
        rt.anchorMax = a;
        rt.pivot = a;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    // ── ScrollView (스킬 문서의 필수 구조대로) ──

    public struct ScrollParts
    {
        public GameObject root;
        public ScrollRect scrollRect;
        public RectTransform viewport;
        public RectTransform content;
    }

    /// <summary>
    /// ScrollView (ScrollRect)
    ///   └ Viewport (Mask + Image)
    ///       └ Content (VerticalLayoutGroup + ContentSizeFitter)
    /// </summary>
    public static ScrollParts ScrollView(string name, Transform parent, float spacing = 18f, RectOffset padding = null)
    {
        GameObject root = Obj(name, parent);
        ScrollRect scroll = root.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        // Viewport: Mask는 Image를 필요로 한다.
        GameObject viewport = Img("Viewport", root.transform, new Color(1f, 1f, 1f, 0.004f), true);
        Stretch(viewport, 0, 0, 0, 0);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content: VLG가 자식 배치, ContentSizeFitter가 Content 자신의 높이를 만든다.
        GameObject content = Obj("Content", viewport.transform);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = padding ?? new RectOffset(24, 24, 20, 20);
        vlg.spacing = spacing;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        // 자식(섹션)의 preferredHeight를 읽어 배치한다.
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Content 자신의 높이는 Fitter가 만든다. Content는 레이아웃 그룹 안에 있지 않으므로 충돌 없음.
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRt;

        return new ScrollParts
        {
            root = root,
            scrollRect = scroll,
            viewport = viewport.GetComponent<RectTransform>(),
            content = contentRt
        };
    }

    /// <summary>VerticalLayoutGroup 자식이 고정 높이를 갖도록 LayoutElement를 붙인다.</summary>
    public static LayoutElement FixedHeight(GameObject go, float height)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
        return le;
    }

    // ── 참조 연결 ──────────────────────────

    /// <summary>[SerializeField] private 필드에 값을 넣는다.</summary>
    public static void SetRef(SerializedObject so, string fieldName, Object value)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"[UIBuildKit] '{fieldName}' 필드를 찾지 못했습니다. 인스펙터에서 직접 연결해주세요.");
            return;
        }
        prop.objectReferenceValue = value;
    }
}
#endif
