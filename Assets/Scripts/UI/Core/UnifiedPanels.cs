using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 10.31 UI 통일 (시안 12번): 일시정지·설정 화면을 실행 중에 새 기준으로 입힌다.
/// 프리팹(System_Canvas)과 타이틀 씬은 고치지 않는다. 창이 처음 열릴 때 한 번만 바꾼다.
/// </summary>
public static class UnifiedPanels
{
    private const string Marker = "UnifiedStyled";

    // ── 일시정지 ─────────────────────────────

    private static readonly (string name, string label)[] PauseItems =
    {
        ("ResumeButton", "계속하기"),
        ("ControlGuideButton", "조작법"),
        ("SettingsButton", "설정"),
        ("QuitButton", "타이틀로"),
    };

    /// <summary>
    /// 일시정지: 게임 화면을 어둡게 덮고(왼쪽이 더 어둡게), 왼쪽에 "일시정지" 제목과 글자 메뉴.
    /// 고른 메뉴는 먹 붓줄 + 달빛 번짐 (타이틀 메뉴와 같다).
    /// </summary>
    public static void StylePause(PausePanel pause)
    {
        if (pause == null) return;
        RectTransform root = (RectTransform)pause.transform;

        if (root.Find(Marker) == null)
        {
            TMP_FontAsset font = UnifiedStyle.FontOf(pause);
            UnifiedStyle.NewRect(Marker, root);

            // 창을 화면 전체로 넓히고, 원래 판 그림은 투명하게. 자동 정렬이 있으면 꺼서 직접 놓는다.
            UnifiedStyle.Stretch(root);
            LayoutGroup layout = pause.GetComponent<LayoutGroup>();
            if (layout != null) layout.enabled = false;
            ContentSizeFitter fitter = pause.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            Image own = pause.GetComponent<Image>();
            if (own != null) own.color = new Color(0f, 0f, 0f, 0f);

            UnifiedStyle.Backdrop(root, 0.9f, 0.55f);
            UnifiedStyle.Title(root, font, "일시정지", 180f, 230f, 58f);

            Button previous = null;
            Button first = null;
            for (int i = 0; i < PauseItems.Length; i++)
            {
                Transform t = root.Find(PauseItems[i].name);
                Button b = t != null ? t.GetComponent<Button>() : null;
                if (b == null) continue;

                RectTransform br = (RectTransform)b.transform;
                UnifiedStyle.PlaceTopLeft(br, 186f, 380f + i * 88f, 440f, 66f);

                Image bg = b.GetComponent<Image>();
                if (bg != null) { bg.color = new Color(0f, 0f, 0f, 0f); bg.raycastTarget = true; }
                b.transition = Selectable.Transition.None;

                TextMeshProUGUI label = b.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = PauseItems[i].label;
                    if (font != null) label.font = font;
                    label.fontSize = 42f;
                    label.characterSpacing = 12f;
                    label.fontStyle = FontStyles.Normal;
                    label.alignment = TextAlignmentOptions.MidlineLeft;
                    UnifiedStyle.Stretch(label.rectTransform);
                    InkSelect.Attach(b, label);
                }

                // ↑↓로 위아래만
                Navigation nav = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = previous };
                b.navigation = nav;
                if (previous != null)
                {
                    Navigation pn = previous.navigation;
                    pn.selectOnDown = b;
                    previous.navigation = pn;
                }
                previous = b;
                if (first == null) first = b;
            }

            UnifiedStyle.Hint(root, font, "↑↓ 고르기   ·   Enter 선택   ·   Esc 계속하기", 186f, 54f);
        }

        // 열 때마다 "계속하기"를 고른 상태로
        Transform resume = root.Find("ResumeButton");
        if (resume != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(resume.gameObject);
    }

    // ── 설정 ────────────────────────────────

    /// <summary>
    /// 설정: 격자 테두리 창(원래 있음)에 제목·글자 탭·달빛 줄 강조를 입힌다.
    /// 드롭다운·화살표 칸은 가는 테두리, 슬라이더는 가는 선 + 작은 마름모, 아래 버튼은 선 상자.
    /// </summary>
    public static void StyleSettings(SettingsPanel settings)
    {
        if (settings == null) return;
        Transform root = settings.transform;
        if (root.Find(Marker) != null) return;
        UnifiedStyle.NewRect(Marker, root);

        TMP_FontAsset font = UnifiedStyle.FontOf(settings);

        Image dim = Find<Image>(root, "Dim");
        if (dim != null) dim.color = new Color(UnifiedStyle.Ink.r, UnifiedStyle.Ink.g, UnifiedStyle.Ink.b, 0.82f);

        Transform window = root.Find("Window");
        if (window == null) return;
        Image windowBg = window.GetComponent<Image>();
        if (windowBg != null) windowBg.color = UnifiedStyle.Panel;

        // 제목 자리: 창을 위로 늘리고 안쪽을 아래로 내려, 왼쪽 위 창살 모서리(약 96px)와 안 겹치게 한다
        RectTransform wr = (RectTransform)window;
        const float titleBand = 80f;
        wr.sizeDelta += new Vector2(0f, titleBand);
        LatticeFrame.Attach(wr, wr.sizeDelta.y);
        foreach (string inner in new[] { "TabRail", "SettingsScroll" })
        {
            RectTransform ir = window.Find(inner) as RectTransform;
            if (ir != null) ir.offsetMax -= new Vector2(0f, titleBand);
        }
        UnifiedStyle.Title(window, font, "설정", 112f, 34f, 46f);

        // 탭: 회색 상자를 빼고 글자 + 먹 붓줄
        Transform rail = window.Find("TabRail");
        if (rail != null)
        {
            Image railBg = rail.GetComponent<Image>();
            if (railBg != null) railBg.color = new Color(0f, 0f, 0f, 0f);
            foreach (Transform tab in rail)
            {
                Button b = tab.GetComponent<Button>();
                if (b == null) continue;
                Image bg = b.GetComponent<Image>();
                if (bg != null) bg.color = new Color(0f, 0f, 0f, 0f);
                b.transition = Selectable.Transition.None;
                TextMeshProUGUI label = tab.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label == null) continue;
                if (font != null) label.font = font;
                label.fontSize = 26f;
                label.characterSpacing = 10f;
                InkSelect.Attach(b, label, false, new Color(0.55f, 0.57f, 0.61f, 1f), UnifiedStyle.MoonText);
            }
        }

        // 섹션 제목·구분선·줄
        foreach (Transform t in window.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Header" && t.parent != null && t.parent.name.StartsWith("Section_"))
            {
                TextMeshProUGUI h = t.GetComponent<TextMeshProUGUI>();
                if (h == null) continue;
                if (font != null) h.font = font;
                h.color = new Color(0.84f, 0.87f, 0.95f, 0.85f);
                h.characterSpacing = 14f;
                h.fontSize = 28f;
            }
            else if (t.name == "Divider")
            {
                Image d = t.GetComponent<Image>();
                if (d != null) d.color = UnifiedStyle.Line;
            }
            else if (t.name.StartsWith("Row_"))
            {
                TextMeshProUGUI label = t.Find("Label") != null ? t.Find("Label").GetComponent<TextMeshProUGUI>() : null;
                if (label != null && font != null) label.font = font;
                RowHighlight.Attach((RectTransform)t, label);
            }
        }

        foreach (TMP_Dropdown dd in window.GetComponentsInChildren<TMP_Dropdown>(true)) StyleDropdown(dd);
        foreach (Slider s in window.GetComponentsInChildren<Slider>(true)) StyleSlider(s);
        foreach (SettingsArrowSelector a in window.GetComponentsInChildren<SettingsArrowSelector>(true)) StyleArrow(a);

        StyleButtonNamed(window, "Button_Apply", UnifiedStyle.ButtonKind.Primary, font);
        StyleButtonNamed(window, "Button_Default", UnifiedStyle.ButtonKind.Normal, font);
        StyleButtonNamed(window, "Button_Back", UnifiedStyle.ButtonKind.Normal, font, "돌아가기");

        // 단축키 설정 같은 줄 안 버튼
        foreach (Transform t in window.GetComponentsInChildren<Transform>(true))
            if (t.name == "Button" && t.parent != null && t.parent.name == "Control")
                UnifiedStyle.StyleButton(t.GetComponent<Button>(), UnifiedStyle.ButtonKind.Normal, font);
    }

    private static void StyleButtonNamed(Transform window, string name, UnifiedStyle.ButtonKind kind, TMP_FontAsset font, string relabel = null)
    {
        Transform t = window.Find(name);
        Button b = t != null ? t.GetComponent<Button>() : null;
        if (b == null) return;
        UnifiedStyle.StyleButton(b, kind, font);
        if (relabel != null)
        {
            TextMeshProUGUI label = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.text = relabel;
        }
    }

    private static void StyleDropdown(TMP_Dropdown dd)
    {
        Image bg = dd.GetComponent<Image>();
        if (bg != null)
        {
            bg.sprite = UnifiedStyle.BorderSprite();
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 2f;
        }
        ColorBlock cb = dd.colors;
        cb.normalColor = UnifiedStyle.Line;
        cb.highlightedColor = UnifiedStyle.Moon;
        cb.selectedColor = UnifiedStyle.Moon;
        cb.pressedColor = UnifiedStyle.Moon;
        cb.colorMultiplier = 1f;
        dd.colors = cb;

        if (dd.captionText != null) dd.captionText.color = UnifiedStyle.Text;
        Transform arrow = dd.transform.Find("Arrow");
        if (arrow != null && arrow.TryGetComponent(out Image a)) a.color = new Color(0.84f, 0.87f, 0.95f, 0.6f);

        if (dd.template != null)
        {
            Image tb = dd.template.GetComponent<Image>();
            if (tb != null)
            {
                tb.sprite = UnifiedStyle.BorderSprite();
                tb.type = Image.Type.Sliced;
                tb.pixelsPerUnitMultiplier = 2f;
                tb.color = UnifiedStyle.LineStrong;
                Image under = UnifiedStyle.NewImage("TemplateFill", dd.template, new Color(0.05f, 0.07f, 0.1f, 0.98f));
                UnifiedStyle.Stretch(under.rectTransform, 1f, 1f, 1f, 1f);
                under.transform.SetSiblingIndex(0);
            }
            if (dd.itemText != null) dd.itemText.color = UnifiedStyle.Text;
            Transform itemBg = dd.template.Find("Viewport/Content/Item/Item Background");
            if (itemBg != null && itemBg.TryGetComponent(out Image ib))
            {
                ib.color = Color.white;
                Toggle toggle = itemBg.GetComponentInParent<Toggle>(true);
                if (toggle != null)
                {
                    ColorBlock tc = toggle.colors;
                    tc.normalColor = new Color(0f, 0f, 0f, 0f);
                    tc.highlightedColor = new Color(0.63f, 0.71f, 1f, 0.18f);
                    tc.selectedColor = new Color(0.63f, 0.71f, 1f, 0.18f);
                    tc.pressedColor = new Color(0.63f, 0.71f, 1f, 0.28f);
                    tc.colorMultiplier = 1f;
                    toggle.colors = tc;
                }
            }
        }
    }

    private static void StyleSlider(Slider s)
    {
        Transform bgT = s.transform.Find("Background");
        if (bgT != null && bgT.TryGetComponent(out Image bg))
        {
            bg.sprite = null;
            bg.color = UnifiedStyle.Line;
            RectTransform r = bg.rectTransform;
            r.anchorMin = new Vector2(0f, 0.5f);
            r.anchorMax = new Vector2(1f, 0.5f);
            r.sizeDelta = new Vector2(0f, 2f);
            r.anchoredPosition = Vector2.zero;
        }

        if (s.fillRect != null)
        {
            Image fill = s.fillRect.GetComponent<Image>();
            if (fill != null) { fill.sprite = null; fill.color = new Color(0.84f, 0.87f, 0.95f, 0.75f); }
            RectTransform area = s.fillRect.parent as RectTransform;
            if (area != null)
            {
                area.anchorMin = new Vector2(0f, 0.5f);
                area.anchorMax = new Vector2(1f, 0.5f);
                area.sizeDelta = new Vector2(area.sizeDelta.x, 2f);
                area.anchoredPosition = new Vector2(area.anchoredPosition.x, 0f);
            }
        }

        if (s.handleRect != null)
        {
            Image handle = s.handleRect.GetComponent<Image>();
            if (handle != null)
            {
                handle.sprite = UnifiedStyle.DiamondSprite();
                handle.color = Color.white;
                handle.type = Image.Type.Simple;
            }
            s.handleRect.sizeDelta = new Vector2(22f, 0f);
            ColorBlock cb = s.colors;
            cb.normalColor = new Color(0.84f, 0.87f, 0.95f, 0.85f);
            cb.highlightedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.pressedColor = Color.white;
            cb.colorMultiplier = 1f;
            s.colors = cb;
        }
    }

    private static void StyleArrow(SettingsArrowSelector selector)
    {
        Image box = selector.GetComponent<Image>();
        if (box != null) box.color = new Color(0f, 0f, 0f, 0f);

        foreach (Button b in selector.GetComponentsInChildren<Button>(true))
        {
            Image bg = b.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0f, 0f, 0f, 0f);
            ColorBlock cb = b.colors;
            b.colors = cb;
            TextMeshProUGUI t = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t == null) continue;
            t.text = b.name.Contains("Left") ? "‹" : "›";
            t.fontSize = 34f;
            t.color = new Color(0.84f, 0.87f, 0.95f, 0.8f);
        }

        Transform value = selector.transform.Find("Value");
        if (value != null && value.TryGetComponent(out TextMeshProUGUI v)) v.color = UnifiedStyle.Text;
    }

    private static T Find<T>(Transform root, string name) where T : Component
    {
        Transform t = root.Find(name);
        return t != null ? t.GetComponent<T>() : null;
    }
}
