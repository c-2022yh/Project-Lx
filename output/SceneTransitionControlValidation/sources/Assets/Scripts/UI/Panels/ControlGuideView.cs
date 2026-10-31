using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 조작법 화면 (10.30 시안 11번, 10.31 UI 통일 기준).
/// 타이틀 "가이드"와 일시정지 "조작법"이 같이 쓴다. 프리팹 없이 열 때마다 코드로 그린다.
///   - 왼쪽: 키보드 그림. 게임에서 쓰는 키만 묶음 색(이동 청자·전투 연주홍·창 연보라)으로 밝힌다.
///   - 오른쪽·아래: 묶음별 목록. 항목을 고르면(마우스·↑↓) 키보드에서 그 키가 빛난다.
/// 키를 바꾸면 아래 Groups 목록만 고치면 된다.
/// </summary>
public class ControlGuideView : MonoBehaviour
{
    private static ControlGuideView current;

    public static bool IsOpen => current != null;

    private Action onClosed;
    private TMP_FontAsset font;
    private RectTransform root;

    private class Key { public string label; public Image box; public Image fill; public TextMeshProUGUI text; public int cat = -1; }
    private readonly Dictionary<string, Key> keys = new Dictionary<string, Key>();
    private readonly List<Button> rows = new List<Button>();
    private string[] hot = new string[0];

    private static readonly Color[] CatColors =
    {
        new Color(0.56f, 0.81f, 0.74f, 1f),   // 이동 (청자)
        new Color(0.88f, 0.64f, 0.58f, 1f),   // 전투 (연주홍)
        new Color(0.73f, 0.71f, 0.88f, 1f),   // 창 (연보라)
    };

    private class Row { public string[] caps; public bool plus; public string label; public string note; public string[] keys; }
    private class Group { public string title; public int cat; public Row[] rows; }

    private static readonly Group[] Groups =
    {
        new Group { title = "이동", cat = 0, rows = new[]
        {
            new Row { caps = new[] { "←", "→" }, label = "이동", keys = new[] { "←", "→" } },
            new Row { caps = new[] { "C" }, label = "점프", note = "길게 누르면 더 높이", keys = new[] { "C" } },
            new Row { caps = new[] { "↓", "C" }, plus = true, label = "발판 아래로 내려가기", keys = new[] { "↓", "C" } },
            new Row { caps = new[] { "Shift" }, label = "대시", keys = new[] { "Shift" } },
            new Row { caps = new[] { "↑" }, label = "조사하기", note = "제단 · 상자", keys = new[] { "↑" } },
        }},
        new Group { title = "전투", cat = 1, rows = new[]
        {
            new Row { caps = new[] { "Z" }, label = "공격", keys = new[] { "Z" } },
            new Row { caps = new[] { "X" }, label = "유물 공격", keys = new[] { "X" } },
            new Row { caps = new[] { "A", "S", "D", "F" }, label = "스킬", note = "스킬창에서 칸에 넣은 것", keys = new[] { "A", "S", "D", "F" } },
            new Row { caps = new[] { "Q" }, label = "각성", note = "기력이 가득 찼을 때", keys = new[] { "Q" } },
        }},
        new Group { title = "창", cat = 2, rows = new[]
        {
            new Row { caps = new[] { "Tab" }, label = "지도", note = "누르고 있는 동안", keys = new[] { "Tab" } },
            new Row { caps = new[] { "I" }, label = "유물", keys = new[] { "I" } },
            new Row { caps = new[] { "K" }, label = "스킬", keys = new[] { "K" } },
            new Row { caps = new[] { "Esc" }, label = "일시정지", keys = new[] { "Esc" } },
        }},
    };

    // ── 열고 닫기 ───────────────────────────

    public static void Open(Action onClosed)
    {
        if (current != null) return;
        GameObject go = new GameObject("[ControlGuide]");
        current = go.AddComponent<ControlGuideView>();
        current.onClosed = onClosed;
        current.Build();
    }

    public static void CloseCurrent()
    {
        if (current != null) current.Close();
    }

    private void Close()
    {
        Action closed = onClosed;
        onClosed = null;
        if (current == this) current = null;
        Destroy(gameObject);
        closed?.Invoke();
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }

    private void Update()
    {
        // 게임 중에는 UIManager가 Esc를 받아 닫는다 (일시정지로 돌아가기). 타이틀에서는 여기서 닫는다.
        Keyboard kb = Keyboard.current;
        if (UIManager.Instance == null && kb != null && (kb.escapeKey.wasPressedThisFrame || kb.xKey.wasPressedThisFrame))
        {
            Close();
            return;
        }

        if (EventSystem.current != null && rows.Count > 0 &&
            (EventSystem.current.currentSelectedGameObject == null || !EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform)))
            EventSystem.current.SetSelectedGameObject(rows[0].gameObject);
    }

    // ── 그리기 ─────────────────────────────

    private void Build()
    {
        TextMeshProUGUI anyText = FindAnyObjectByType<TextMeshProUGUI>();
        font = anyText != null ? anyText.font : TMP_Settings.defaultFontAsset;

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 460;   // 일시정지·설정 위, 획득 알림(500) 아래
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        root = UnifiedStyle.NewRect("Root", transform);
        UnifiedStyle.Stretch(root);
        UnifiedStyle.Backdrop(root, 0.94f, 0.86f);

        UnifiedStyle.Title(root, font, "조작법", 120f, 78f, 52f);
        TextMeshProUGUI sub = UnifiedStyle.NewText("Sub", root, font, "항목에 마우스를 올리거나 ↑↓로 고르면 키보드에서 위치가 밝아져요.", 22f, UnifiedStyle.TextMuted);
        UnifiedStyle.PlaceTopLeft(sub.rectTransform, 122f, 180f, 1400f, 34f);

        BuildKeyboard(120f, 260f);

        // 오른쪽: 이동·전투 / 왼쪽 아래: 창
        float y = 236f;
        for (int g = 0; g < 2; g++) y = BuildGroup(Groups[g], 1080f, y, 720f) + 28f;
        BuildGroup(Groups[2], 120f, 700f, 840f);

        // ↑↓로 모든 항목을 차례로
        for (int i = 0; i < rows.Count; i++)
        {
            Navigation nav = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = rows[(i - 1 + rows.Count) % rows.Count],
                selectOnDown = rows[(i + 1) % rows.Count]
            };
            rows[i].navigation = nav;
        }

        Button back = NewButton("Button_Back", root, "돌아가기", 220f, 60f);
        RectTransform br = (RectTransform)back.transform;
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(1f, 0f);
        br.anchoredPosition = new Vector2(-120f, 40f);
        back.navigation = new Navigation { mode = Navigation.Mode.None };
        back.onClick.AddListener(() =>
        {
            if (UIManager.Instance != null) UIManager.Instance.CloseControlGuide();
            else Close();
        });

        UnifiedStyle.Hint(root, font, "↑↓ 고르기   ·   Esc 돌아가기", 122f, 48f);

        RefreshKeys();
        if (EventSystem.current != null && rows.Count > 0) EventSystem.current.SetSelectedGameObject(rows[0].gameObject);
    }

    private void BuildKeyboard(float x0, float y0)
    {
        const float u = 63f, gap = 9f;
        RectTransform board = UnifiedStyle.NewRect("Keyboard", root);
        UnifiedStyle.PlaceTopLeft(board, x0, y0, 860f, 380f);

        void Add(string label, float col, float row, float w = 1f)
        {
            float px = col * (u + gap), py = row * (u + gap);
            float width = w * u + (w - 1f) * gap;
            Key k = new Key { label = label };
            k.fill = UnifiedStyle.NewImage("Key_" + label, board, Color.clear);
            UnifiedStyle.PlaceTopLeft(k.fill.rectTransform, px, py, width, u);
            k.box = UnifiedStyle.NewImage("Edge", k.fill.transform, Color.white, UnifiedStyle.BorderSprite());
            k.box.type = Image.Type.Sliced;
            k.box.pixelsPerUnitMultiplier = 2f;
            UnifiedStyle.Stretch(k.box.rectTransform);
            k.text = UnifiedStyle.NewText("Label", k.fill.transform, font, label, label.Length > 2 ? 18f : 24f, Color.white, TextAlignmentOptions.Center);
            UnifiedStyle.Stretch(k.text.rectTransform);
            keys[label] = k;
        }

        Add("Esc", 0, 0);
        Add("Tab", 0, 1, 1.5f);
        string top = "QWERTYUIOP";
        for (int i = 0; i < top.Length; i++) Add(top[i].ToString(), 1.5f + i + 0.05f, 1);
        Add("Caps", 0, 2, 1.75f);
        string mid = "ASDFGHJKL";
        for (int i = 0; i < mid.Length; i++) Add(mid[i].ToString(), 1.75f + i + 0.05f, 2);
        Add("Shift", 0, 3, 2.25f);
        string bot = "ZXCVBNM";
        for (int i = 0; i < bot.Length; i++) Add(bot[i].ToString(), 2.25f + i + 0.1f, 3);
        Add("↑", 10.1f, 2.92f);
        Add("←", 9.1f, 3.92f);
        Add("↓", 10.1f, 3.92f);
        Add("→", 11.1f, 3.92f);

        foreach (Group g in Groups)
            foreach (Row r in g.rows)
                foreach (string k in r.keys)
                    if (keys.TryGetValue(k, out Key key) && key.cat < 0) key.cat = g.cat;
    }

    private float BuildGroup(Group g, float x, float y, float width)
    {
        Color c = CatColors[g.cat];

        TextMeshProUGUI title = UnifiedStyle.NewText("Group_" + g.title, root, font, g.title, 26f, c);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 20f;
        UnifiedStyle.PlaceTopLeft(title.rectTransform, x, y, 200f, 40f);
        Image line = UnifiedStyle.NewImage("GroupLine", root, new Color(c.r, c.g, c.b, 0.35f), UnifiedStyle.BandSprite());
        UnifiedStyle.PlaceTopLeft(line.rectTransform, x + title.GetPreferredValues().x + 20f, y + 20f, width - 120f, 1.5f);
        y += 50f;

        foreach (Row r in g.rows)
        {
            RectTransform row = UnifiedStyle.NewRect("Row_" + r.label, root);
            UnifiedStyle.PlaceTopLeft(row, x, y, width, 48f);
            Image hit = row.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            Button b = row.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = hit;

            float cx = 14f;
            for (int i = 0; i < r.caps.Length; i++)
            {
                if (r.plus && i > 0)
                {
                    TextMeshProUGUI plus = UnifiedStyle.NewText("Plus", row, font, "+", 18f, UnifiedStyle.TextMuted, TextAlignmentOptions.Center);
                    UnifiedStyle.PlaceTopLeft(plus.rectTransform, cx, 8f, 18f, 32f);
                    cx += 22f;
                }
                string cap = r.caps[i];
                float w = cap.Length > 2 ? 64f : 40f;
                Image capBox = UnifiedStyle.NewImage("Cap", row, new Color(c.r, c.g, c.b, 0.45f), UnifiedStyle.BorderSprite());
                capBox.type = Image.Type.Sliced;
                capBox.pixelsPerUnitMultiplier = 2f;
                UnifiedStyle.PlaceTopLeft(capBox.rectTransform, cx, 8f, w, 34f);
                TextMeshProUGUI capText = UnifiedStyle.NewText("Text", capBox.transform, font, cap, 18f, UnifiedStyle.Text, TextAlignmentOptions.Center);
                UnifiedStyle.Stretch(capText.rectTransform);
                cx += w + 6f;
            }

            TextMeshProUGUI label = UnifiedStyle.NewText("Label", row, font, r.label, 26f, UnifiedStyle.Text);
            UnifiedStyle.PlaceTopLeft(label.rectTransform, 214f, 4f, 360f, 40f);
            if (!string.IsNullOrEmpty(r.note))
            {
                TextMeshProUGUI note = UnifiedStyle.NewText("Note", row, font, r.note, 18f, UnifiedStyle.TextMuted);
                UnifiedStyle.PlaceTopLeft(note.rectTransform, 214f + label.GetPreferredValues().x + 16f, 10f, 360f, 30f);
            }

            RowHighlight.Attach(row, label);

            string[] rowKeys = r.keys;
            EventTrigger trigger = row.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry sel = new EventTrigger.Entry { eventID = EventTriggerType.Select };
            sel.callback.AddListener(_ => { hot = rowKeys; RefreshKeys(); });
            trigger.triggers.Add(sel);
            EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => { if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(row.gameObject); });
            trigger.triggers.Add(enter);

            rows.Add(b);
            y += 52f;
        }
        return y;
    }

    private void RefreshKeys()
    {
        bool any = hot != null && hot.Length > 0;
        foreach (Key k in keys.Values)
        {
            bool isHot = any && Array.IndexOf(hot, k.label) >= 0;
            if (k.cat < 0)
            {
                k.box.color = new Color(0.93f, 0.91f, 0.89f, 0.10f);
                k.fill.color = new Color(1f, 1f, 1f, 0.02f);
                k.text.color = new Color(0.93f, 0.91f, 0.89f, 0.22f);
                continue;
            }

            Color c = CatColors[k.cat];
            if (isHot)
            {
                k.box.color = c;
                k.fill.color = new Color(c.r, c.g, c.b, 0.38f);
                k.text.color = Color.white;
            }
            else if (any)
            {
                k.box.color = new Color(c.r, c.g, c.b, 0.35f);
                k.fill.color = new Color(0.07f, 0.09f, 0.11f, 0.85f);
                k.text.color = new Color(0.93f, 0.91f, 0.89f, 0.55f);
            }
            else
            {
                k.box.color = c;
                k.fill.color = new Color(c.r, c.g, c.b, 0.16f);
                k.text.color = new Color(0.95f, 0.94f, 0.91f, 1f);
            }
        }
    }

    private Button NewButton(string name, Transform parent, string label, float w, float h)
    {
        RectTransform rt = UnifiedStyle.NewRect(name, parent);
        rt.sizeDelta = new Vector2(w, h);
        Image bg = rt.gameObject.AddComponent<Image>();
        bg.raycastTarget = true;
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = bg;
        TextMeshProUGUI t = UnifiedStyle.NewText("Label", rt, font, label, 26f, UnifiedStyle.Text, TextAlignmentOptions.Center);
        UnifiedStyle.Stretch(t.rectTransform);
        UnifiedStyle.StyleButton(b, UnifiedStyle.ButtonKind.Normal, font);
        return b;
    }
}
