#if UNITY_EDITOR && LUDENS_UI_TOOLS
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
    public const string KoreanFontPath = "Assets/TextMesh Pro/Fonts/NanumSquare_acB SDF.asset";

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

    // ── 10.16 디자이너 시안 리소스 ─────────────

    public const string RedesignFolder = "Assets/Sprites/UI/Redesign/";

    /// <summary>Sprites/UI/Redesign 폴더의 그림. 이름만 넘기면 된다 (확장자 빼고).</summary>
    public static Sprite Redesign(string name)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RedesignFolder + name + ".png");
        if (sprite == null) Debug.LogWarning("[UIBuildKit] 시안 그림을 찾지 못했습니다: " + RedesignFolder + name + ".png");
        return sprite;
    }

    /// <summary>시안의 빈 칸 테두리 색 (보관함·보유 목록).</summary>
    public static readonly Color EmptyFrameBorder = new Color(1f, 1f, 1f, 0.16f);
    public static readonly Color EmptyFrameFill = new Color(0.10f, 0.10f, 0.11f, 1f);

    /// <summary>
    /// 격자 뒤에 빈 칸 틀을 깔아둔다. 시안처럼 보관함이 비어 있어도 칸 모양이 보이게.
    /// 실제 칸은 같은 자리·같은 격자 설정의 형제 오브젝트가 그 위에 덮는다.
    /// </summary>
    public static GameObject FrameBackdrop(Transform parent, string name, GridLayoutGroup like, int count)
    {
        GameObject root = Obj(name, parent);
        RectTransform src = like.GetComponent<RectTransform>();
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = src.anchorMin;
        rt.anchorMax = src.anchorMax;
        rt.pivot = src.pivot;
        rt.anchoredPosition = src.anchoredPosition;
        rt.sizeDelta = src.sizeDelta;

        GridLayoutGroup grid = root.AddComponent<GridLayoutGroup>();
        grid.cellSize = like.cellSize;
        grid.spacing = like.spacing;
        grid.startCorner = like.startCorner;
        grid.startAxis = like.startAxis;
        grid.childAlignment = like.childAlignment;
        grid.constraint = like.constraint;
        grid.constraintCount = like.constraintCount;

        Sprite fill = Redesign("Frame_Square_Fill");
        Sprite border = Redesign("Frame_Square_Border");

        for (int i = 0; i < count; i++)
        {
            GameObject cell = Img("EmptyCell", root.transform, EmptyFrameFill);
            cell.GetComponent<UnityEngine.UI.Image>().sprite = fill;

            GameObject line = Img("Border", cell.transform, EmptyFrameBorder);
            line.GetComponent<UnityEngine.UI.Image>().sprite = border;
            Stretch(line, 0, 0, 0, 0);
        }

        // 실제 칸보다 먼저 그려져야 한다.
        root.transform.SetSiblingIndex(like.transform.GetSiblingIndex());
        return root;
    }

    // ── 탭 줄 ────────────────────────────────
    //
    // 유물창과 스킬창은 같은 크기의 창 두 개이고 Q/E로 오간다.
    // 두 창이 똑같은 탭 줄을 같은 자리에 그려야 전환할 때 튀지 않는다.
    // 창마다 제목을 따로 쓰는 대신 이 함수를 부른다.
    //
    // 10.16 시안: 상자 없이 글자만. 고른 탭은 밝은 글자 + 아래에서 푸른 빛이 번진다.
    // 두 탭 모두 글자 아래 짧은 밑줄이 있고, 고른 쪽만 밝다.

    private const float TabWidth = 130f;
    private const float TabHeight = 72f;
    private const float TabGap = 16f;

    /// <summary>
    /// 창 왼쪽 위에 "유물 / 스킬" 탭 줄을 그린다.
    /// activeIndex는 UITabBar.RelicTab 또는 UITabBar.SkillTab.
    /// </summary>
    public static UITabBar TabBar(Transform window, int activeIndex)
    {
        // 키 안내는 창 오른쪽 위 WindowHint가 맡는다.
        GameObject bar = Obj("TabBar", window);
        Place(bar, Anchor.TopLeft, 40, -14, TabWidth * 2 + TabGap, TabHeight);

        UITabBar comp = bar.AddComponent<UITabBar>();

        UnityEngine.UI.Button relic = TabButton(bar.transform, "Tab_Relic", "유물", 0,
                                                activeIndex == UITabBar.RelicTab);
        UnityEngine.UI.Button skill = TabButton(bar.transform, "Tab_Skill", "스킬", 1,
                                                activeIndex == UITabBar.SkillTab);

        UnityEditor.Events.UnityEventTools.AddPersistentListener(relic.onClick, comp.OnRelicTab);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(skill.onClick, comp.OnSkillTab);

        return comp;
    }

    private static UnityEngine.UI.Button TabButton(Transform bar, string name, string label,
                                                   int slot, bool active)
    {
        GameObject go = Obj(name, bar);
        Place(go, Anchor.MiddleLeft, slot * (TabWidth + TabGap) + TabWidth / 2f, 0,
              TabWidth, TabHeight);

        // 누를 자리. 보이지 않는다.
        UnityEngine.UI.Image hit = go.AddComponent<UnityEngine.UI.Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        hit.raycastTarget = true;

        UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = hit;

        if (active)
        {
            GameObject glow = Img("Glow", go.transform, Color.white);
            glow.GetComponent<UnityEngine.UI.Image>().sprite = Redesign("Tab_Glow");
            Place(glow, Anchor.BottomCenter, 0, 2, TabWidth + 40f, 60f);
        }

        GameObject text = Text("Label", go.transform, label, 38,
            active ? FontStyles.Bold : FontStyles.Normal, TextAlignmentOptions.Center,
            active ? Color.white : new Color(0.55f, 0.56f, 0.60f, 1f));
        Stretch(text, 0, 0, 0, 10);

        GameObject dash = Img("Underline", go.transform,
            active ? new Color(0.85f, 0.92f, 1f, 1f) : new Color(1f, 1f, 1f, 0.25f));
        Place(dash, Anchor.BottomCenter, 0, 2, 30, 3);

        return btn;
    }
}
#endif
