using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 저장 슬롯 선택 화면 ("게임 기록").
/// 슬롯 정보는 SaveSlotService.Provider에서 가져오므로, 세이브 시스템이 바뀌어도 이 스크립트는 고칠 게 없다.
///
/// 10.31 UI 통일 (시안 12번): 화면을 코드로 새로 그린다. 빌더가 만든 예전 회색 상자들은 숨긴다.
///   - 타이틀 그림을 어둡게 깔고, 왼쪽에 슬롯 카드, 오른쪽에 고른 슬롯의 자세한 정보 (격자 테두리 창).
///   - 고른 카드는 테두리가 밝아지고 이름 아래 먹 붓줄이 그어진다. 마우스를 올려도 골라진다.
///   - ↑↓ 고르기 · Enter 시작 · Delete 지우기 · Esc 돌아가기.
/// </summary>
public class SaveSlotPanel : MonoBehaviour
{
    [Header("예전 목록 (10.31부터 안 씀, 참조만 남겨 둠)")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotEntryPrefab;

    // ── 배치 (1920×1080 기준) ──
    private const float Left = 120f, Top = 236f;
    private const float CardWidth = 980f, CardMaxHeight = 176f, CardGap = 26f, ListHeight = 580f;
    private const float DetailLeft = 1180f, DetailWidth = 620f;

    private TitleMenuPanel returnTo;
    private TMP_FontAsset font;
    private RectTransform root;
    private bool built;

    private class Card
    {
        public int index;
        public SaveSlotInfo info;
        public Button button;
        public Image border, fill, glow;
        public TextMeshProUGUI number, name, detail, saved;
    }

    private readonly List<Card> cards = new List<Card>();
    private int current;

    // 오른쪽 자세한 정보
    private RectTransform detail;
    private GameObject filledGroup, emptyGroup;
    private Image thumb;
    private TextMeshProUGUI place;
    private readonly List<TextMeshProUGUI> statKeys = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> statValues = new List<TextMeshProUGUI>();
    private Button primaryButton, deleteButton;
    private TextMeshProUGUI primaryLabel;
    private Sprite fallbackThumb;

    // 삭제 확인 창
    private GameObject confirmRoot;
    private TextMeshProUGUI confirmText;
    private Button confirmDeleteButton;
    private Button confirmCancelButton;
    private int pendingDeleteSlot = -1;

    /// <summary>삭제 확인 창이 떠 있는지. 떠 있는 동안 Esc는 창을 닫는 데만 쓴다.</summary>
    public bool IsConfirmOpen => confirmRoot != null && confirmRoot.activeSelf;

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    /// <summary>타이틀에서 열 때 호출. 뒤로가기 목적지를 같이 받는다.</summary>
    public void Open(TitleMenuPanel from)
    {
        returnTo = from;
        SetVisible(true);
        Refresh();
    }

    /// <summary>슬롯 목록을 다시 그린다.</summary>
    public void Refresh()
    {
        if (!built) Build();

        foreach (Card c in cards)
            if (c.button != null) Destroy(c.button.gameObject);
        cards.Clear();

        ISaveSlotProvider provider = SaveSlotService.Provider;
        int count = provider.SlotCount;
        float h = Mathf.Min(CardMaxHeight, (ListHeight - CardGap * (count - 1)) / Mathf.Max(1, count));

        for (int i = 0; i < count; i++)
            cards.Add(MakeCard(i, provider.GetSlot(i), Top + i * (h + CardGap), h));

        // ↑↓로 카드 사이를 오가고, →로 오른쪽 정보창 버튼(이어하기·지우기)으로 넘어간다
        for (int i = 0; i < cards.Count; i++)
        {
            Navigation nav = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = i > 0 ? cards[i - 1].button : null,
                selectOnDown = i < cards.Count - 1 ? cards[i + 1].button : null,
                selectOnRight = primaryButton
            };
            cards[i].button.navigation = nav;
        }

        float listBottom = cards.Count > 0 ? Top + cards.Count * (h + CardGap) - CardGap : Top + ListHeight;
        UnifiedStyle.PlaceTopLeft(detail, DetailLeft, Top, DetailWidth, listBottom - Top);

        Choose(Mathf.Clamp(current, 0, Mathf.Max(0, cards.Count - 1)), true);
    }

    // ── 화면 만들기 ──────────────────────────

    private void Build()
    {
        built = true;
        font = UnifiedStyle.FontOf(this);

        // 빌더가 만든 예전 화면은 숨긴다 (지우지는 않는다)
        foreach (Transform child in transform) child.gameObject.SetActive(false);

        root = UnifiedStyle.NewRect("Unified", transform);
        UnifiedStyle.Stretch(root);

        // 타이틀 그림을 어둡게 깐다 (타이틀에서 넘어와도 분위기가 이어지게)
        Sprite titleArt = FindTitleArt();
        fallbackThumb = titleArt;
        if (titleArt != null)
        {
            Image art = UnifiedStyle.NewImage("TitleArt", root, new Color(0.6f, 0.62f, 0.68f, 1f), titleArt);
            UnifiedStyle.Stretch(art.rectTransform);
            AspectRatioFitter fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = titleArt.rect.width / titleArt.rect.height;
        }
        Image shade = UnifiedStyle.Backdrop(root, 0.93f, 0.84f);
        shade.transform.SetSiblingIndex(titleArt != null ? 1 : 0);

        UnifiedStyle.Title(root, font, "게임 기록", Left, 84f, 52f);

        BuildDetail();

        Button back = MakeButton("Button_Back", root, "돌아가기", UnifiedStyle.ButtonKind.Normal, 220f, 60f);
        RectTransform br = (RectTransform)back.transform;
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(1f, 0f);
        br.anchoredPosition = new Vector2(-120f, 36f);
        back.onClick.AddListener(OnBackButton);
        Navigation none = new Navigation { mode = Navigation.Mode.None };
        back.navigation = none;

        UnifiedStyle.Hint(root, font, "↑↓ 고르기   ·   → 버튼으로   ·   Enter 선택   ·   Delete 지우기   ·   Esc 돌아가기", Left, 48f);
    }

    private void BuildDetail()
    {
        detail = UnifiedStyle.NewRect("Detail", root);
        Image bg = detail.gameObject.AddComponent<Image>();
        bg.color = UnifiedStyle.Panel;
        bg.raycastTarget = false;
        LatticeFrame.Attach(detail, ListHeight, null, 10f, 0.75f);

        // 기록이 있을 때
        filledGroup = UnifiedStyle.NewRect("Filled", detail).gameObject;
        UnifiedStyle.Stretch((RectTransform)filledGroup.transform);

        Image thumbFrame = UnifiedStyle.NewImage("ThumbFrame", filledGroup.transform, UnifiedStyle.Line, UnifiedStyle.BorderSprite());
        thumbFrame.type = Image.Type.Sliced;
        thumbFrame.pixelsPerUnitMultiplier = 2f;
        UnifiedStyle.PlaceTopLeft(thumbFrame.rectTransform, 40f, 40f, DetailWidth - 80f, 190f);
        thumb = UnifiedStyle.NewImage("Thumb", thumbFrame.transform, Color.white, null);
        UnifiedStyle.Stretch(thumb.rectTransform, 1f, 1f, 1f, 1f);
        thumb.preserveAspect = false;

        place = UnifiedStyle.NewText("Place", filledGroup.transform, font, "", 36f, UnifiedStyle.MoonText);
        place.fontStyle = FontStyles.Bold;
        place.characterSpacing = 12f;
        UnifiedStyle.PlaceTopLeft(place.rectTransform, 40f, 254f, DetailWidth - 80f, 50f);

        for (int i = 0; i < 3; i++)
        {
            TextMeshProUGUI k = UnifiedStyle.NewText("Key" + i, filledGroup.transform, font, "", 22f, UnifiedStyle.TextMuted);
            UnifiedStyle.PlaceTopLeft(k.rectTransform, 40f, 320f + i * 40f, 260f, 34f);
            TextMeshProUGUI v = UnifiedStyle.NewText("Value" + i, filledGroup.transform, font, "", 22f, UnifiedStyle.Text, TextAlignmentOptions.MidlineRight);
            UnifiedStyle.PlaceTopLeft(v.rectTransform, DetailWidth - 340f, 320f + i * 40f, 300f, 34f);
            statKeys.Add(k);
            statValues.Add(v);
        }

        // 비어 있을 때
        emptyGroup = UnifiedStyle.NewRect("Empty", detail).gameObject;
        UnifiedStyle.Stretch((RectTransform)emptyGroup.transform);
        TextMeshProUGUI plus = UnifiedStyle.NewText("Plus", emptyGroup.transform, font, "+", 96f, new Color(0.84f, 0.87f, 0.95f, 0.5f), TextAlignmentOptions.Center);
        UnifiedStyle.PlaceTopLeft(plus.rectTransform, 0f, 120f, DetailWidth, 110f);
        TextMeshProUGUI et = UnifiedStyle.NewText("EmptyTitle", emptyGroup.transform, font, "새로 시작하기", 34f, UnifiedStyle.Text, TextAlignmentOptions.Center);
        et.characterSpacing = 12f;
        UnifiedStyle.PlaceTopLeft(et.rectTransform, 0f, 250f, DetailWidth, 50f);
        TextMeshProUGUI es = UnifiedStyle.NewText("EmptySub", emptyGroup.transform, font, "시작 연출부터 봅니다", 22f, UnifiedStyle.TextMuted, TextAlignmentOptions.Center);
        UnifiedStyle.PlaceTopLeft(es.rectTransform, 0f, 306f, DetailWidth, 34f);

        // 아래 버튼 (둘 다 공통)
        primaryButton = MakeButton("Button_Primary", detail, "이어하기", UnifiedStyle.ButtonKind.Primary, 380f, 64f);
        RectTransform pr = (RectTransform)primaryButton.transform;
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0f, 0f);
        pr.anchoredPosition = new Vector2(40f, 36f);
        primaryLabel = primaryButton.GetComponentInChildren<TextMeshProUGUI>(true);
        primaryButton.onClick.AddListener(() => OnSlotClicked(current));

        deleteButton = MakeButton("Button_Delete", detail, "지우기", UnifiedStyle.ButtonKind.Danger, 140f, 64f);
        RectTransform dr = (RectTransform)deleteButton.transform;
        dr.anchorMin = dr.anchorMax = dr.pivot = new Vector2(1f, 0f);
        dr.anchoredPosition = new Vector2(-40f, 36f);
        deleteButton.onClick.AddListener(() => OnDeleteClicked(current));

        Navigation none = new Navigation { mode = Navigation.Mode.None };
        primaryButton.navigation = none;
        deleteButton.navigation = none;
    }

    private Card MakeCard(int index, SaveSlotInfo info, float y, float height)
    {
        Card c = new Card { index = index, info = info };
        bool filled = info != null && !info.isEmpty;

        RectTransform rt = UnifiedStyle.NewRect("Slot_" + (index + 1), root);
        UnifiedStyle.PlaceTopLeft(rt, Left, y, CardWidth, height);

        c.fill = rt.gameObject.AddComponent<Image>();   // 버튼이 받는 판 (투명에 가까운 먹색)
        c.fill.color = new Color(UnifiedStyle.Panel.r, UnifiedStyle.Panel.g, UnifiedStyle.Panel.b, 0.78f);
        c.fill.raycastTarget = true;

        c.glow = UnifiedStyle.NewImage("Glow", rt, UnifiedStyle.Glow, UnifiedStyle.GlowSprite());
        RectTransform gr = c.glow.rectTransform;
        gr.anchorMin = gr.anchorMax = new Vector2(0f, 0.5f);
        gr.sizeDelta = new Vector2(760f, height * 1.6f);
        gr.anchoredPosition = new Vector2(80f, 0f);

        c.border = UnifiedStyle.NewImage("Border", rt, Color.white, UnifiedStyle.BorderSprite());
        c.border.type = Image.Type.Sliced;
        c.border.pixelsPerUnitMultiplier = 2f;
        UnifiedStyle.Stretch(c.border.rectTransform);
        Image inner = UnifiedStyle.NewImage("Inner", rt, new Color(0.88f, 0.90f, 0.97f, 0.08f), UnifiedStyle.BorderSprite());
        inner.type = Image.Type.Sliced;
        inner.pixelsPerUnitMultiplier = 2f;
        UnifiedStyle.Stretch(inner.rectTransform, 10f, 10f, 10f, 10f);

        c.number = UnifiedStyle.NewText("Number", rt, font, (index + 1).ToString(), height * 0.62f, UnifiedStyle.TextMuted, TextAlignmentOptions.Center);
        RectTransform nr = c.number.rectTransform;
        nr.anchorMin = nr.anchorMax = new Vector2(0f, 0.5f);
        nr.sizeDelta = new Vector2(120f, height);
        nr.anchoredPosition = new Vector2(84f, 0f);

        c.name = UnifiedStyle.NewText("Name", rt, font, "", 38f, UnifiedStyle.Text);
        c.name.characterSpacing = 10f;
        RectTransform nm = c.name.rectTransform;
        nm.anchorMin = nm.anchorMax = new Vector2(0f, 0.5f);
        nm.pivot = new Vector2(0f, 0.5f);
        nm.sizeDelta = new Vector2(620f, 50f);
        nm.anchoredPosition = new Vector2(170f, height * 0.16f);

        c.detail = UnifiedStyle.NewText("Detail", rt, font, "", 22f, UnifiedStyle.TextMuted);
        RectTransform dt = c.detail.rectTransform;
        dt.anchorMin = dt.anchorMax = new Vector2(0f, 0.5f);
        dt.pivot = new Vector2(0f, 0.5f);
        dt.sizeDelta = new Vector2(700f, 34f);
        dt.anchoredPosition = new Vector2(172f, -height * 0.2f);

        c.saved = UnifiedStyle.NewText("Saved", rt, font, "", 20f, new Color(UnifiedStyle.Text.r, UnifiedStyle.Text.g, UnifiedStyle.Text.b, 0.4f), TextAlignmentOptions.TopRight);
        RectTransform sv = c.saved.rectTransform;
        sv.anchorMin = sv.anchorMax = sv.pivot = new Vector2(1f, 1f);
        sv.sizeDelta = new Vector2(300f, 30f);
        sv.anchoredPosition = new Vector2(-30f, -24f);

        if (filled)
        {
            c.name.text = !string.IsNullOrEmpty(info.locationName) ? info.locationName
                : (string.IsNullOrEmpty(info.slotName) ? $"기록 {index + 1}" : info.slotName);
            c.detail.text = $"플레이 시간 {info.PlayTimeText}";
            c.saved.text = info.SavedAtText;
        }
        else
        {
            c.name.text = "비어 있음";
            c.detail.text = "새로 시작하기";
        }

        c.button = rt.gameObject.AddComponent<Button>();
        c.button.targetGraphic = c.border;
        c.button.transition = Selectable.Transition.None;   // 모양은 Choose가 직접 바꾼다
        int captured = index;
        c.button.onClick.AddListener(() => OnSlotClicked(captured));

        InkSelect.Attach(c.button, c.name, false,
            filled ? new Color(0.79f, 0.80f, 0.83f, 1f) : new Color(0.93f, 0.91f, 0.89f, 0.45f), UnifiedStyle.MoonText);

        EventTrigger trigger = rt.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry onSelect = new EventTrigger.Entry { eventID = EventTriggerType.Select };
        onSelect.callback.AddListener(_ => Choose(captured, false));
        trigger.triggers.Add(onSelect);

        return c;
    }

    // ── 고르기 ─────────────────────────────

    private void Choose(int index, bool selectToo)
    {
        if (cards.Count == 0) return;
        current = Mathf.Clamp(index, 0, cards.Count - 1);

        for (int i = 0; i < cards.Count; i++)
        {
            Card c = cards[i];
            bool on = i == current;
            c.border.color = on ? UnifiedStyle.LineStrong : UnifiedStyle.Line;
            c.fill.color = new Color(UnifiedStyle.Panel.r, UnifiedStyle.Panel.g, UnifiedStyle.Panel.b, on ? 0.94f : 0.78f);
            c.glow.enabled = on;
            c.number.color = on ? new Color(0.84f, 0.87f, 0.95f, 0.85f) : new Color(0.84f, 0.87f, 0.95f, 0.25f);
        }

        ShowDetail(cards[current].info);
        LinkDetailButtons();

        if (selectToo && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(cards[current].button.gameObject);
    }

    /// <summary>
    /// 정보창 버튼 사이 길: 이어하기 ←→ 지우기, 이어하기에서 ←는 지금 고른 카드로 돌아간다.
    /// 빈 슬롯이면 지우기가 없으니 이어하기(새로 시작)만.
    /// </summary>
    private void LinkDetailButtons()
    {
        if (primaryButton == null || deleteButton == null || cards.Count == 0) return;
        Button card = cards[current].button;
        bool canDelete = deleteButton.gameObject.activeSelf;

        primaryButton.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnLeft = card,
            selectOnRight = canDelete ? deleteButton : null
        };
        deleteButton.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnLeft = primaryButton
        };
    }

    private void ShowDetail(SaveSlotInfo info)
    {
        bool filled = info != null && !info.isEmpty;
        filledGroup.SetActive(filled);
        emptyGroup.SetActive(!filled);
        bool challenge = filled && SaveSystem.IsReadOnlySlot(info.slotIndex);
        deleteButton.gameObject.SetActive(filled && !challenge);
        primaryLabel.text = filled ? "이어하기" : "새로 시작";

        if (challenge) primaryLabel.text = "\uB3C4\uC804 \uC2DC\uC791";

        if (!filled) return;

        Sprite s = info.thumbnail != null ? info.thumbnail : fallbackThumb;
        thumb.sprite = s;
        thumb.color = s != null ? new Color(0.85f, 0.87f, 0.92f, 1f) : new Color(0.1f, 0.12f, 0.15f, 1f);
        place.text = !string.IsNullOrEmpty(info.locationName) ? info.locationName : $"기록 {info.slotIndex + 1}";

        string[,] rows =
        {
            { "플레이 시간", info.PlayTimeText },
            { "마지막 저장", info.SavedAtText },
            { info.level > 0 ? "레벨" : "", info.level > 0 ? info.level.ToString() : "" },
        };
        for (int i = 0; i < statKeys.Count; i++)
        {
            statKeys[i].text = rows[i, 0];
            statValues[i].text = rows[i, 1];
        }
    }

    // ── 버튼 ───────────────────────────────

    private void OnSlotClicked(int slotIndex)
    {
        if (IsConfirmOpen) return;

        ISaveSlotProvider provider = SaveSlotService.Provider;
        SaveSlotInfo info = provider.GetSlot(slotIndex);

        if (info != null && info.isEmpty)
            provider.StartNewGame(slotIndex);
        else
            provider.LoadSlot(slotIndex);
    }

    /// <summary>"+ 새 게임 시작" — 비어 있는 첫 슬롯을 쓴다.</summary>
    public void OnNewGameButton()
    {
        ISaveSlotProvider provider = SaveSlotService.Provider;

        for (int i = 0; i < provider.SlotCount; i++)
        {
            SaveSlotInfo info = provider.GetSlot(i);
            if (info == null || !info.isEmpty) continue;

            provider.StartNewGame(i);
            return;
        }

        Debug.Log("[SaveSlotPanel] 빈 슬롯이 없습니다. 덮어쓸 슬롯을 직접 선택해주세요.");
    }

    /// <summary>"돌아가기"</summary>
    public void OnBackButton()
    {
        if (IsConfirmOpen) CloseConfirm();
        SetVisible(false);
        returnTo?.SetVisible(true);
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (IsConfirmOpen)
        {
            if (kb.escapeKey.wasPressedThisFrame || kb.xKey.wasPressedThisFrame) CloseConfirm();
            return;
        }

        if (kb.escapeKey.wasPressedThisFrame) { OnBackButton(); return; }
        if (kb.deleteKey.wasPressedThisFrame) OnDeleteClicked(current);

        // 빈 곳을 눌러 선택이 풀리면 고른 카드로 되돌린다 (키보드로 계속 움직일 수 있게)
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && cards.Count > 0)
            EventSystem.current.SetSelectedGameObject(cards[current].button.gameObject);
    }

    private void OnDisable()
    {
        if (IsConfirmOpen) CloseConfirm();
    }

    // ── 슬롯 삭제 ───────────────────────────

    private void OnDeleteClicked(int slotIndex)
    {
        if (SaveSystem.IsReadOnlySlot(slotIndex)) return;
        SaveSlotInfo info = SaveSlotService.Provider.GetSlot(slotIndex);
        if (info == null || info.isEmpty) return;

        if (confirmRoot == null) BuildConfirm();

        pendingDeleteSlot = slotIndex;
        string where = string.IsNullOrEmpty(info.locationName) ? "" : $" · {info.locationName}";
        confirmText.text = $"기록 {slotIndex + 1}을 지울까요?\n" +
                           $"<size=70%><color=#9AA3AD>플레이 시간 {info.PlayTimeText}{where}\n지우면 되돌릴 수 없습니다.</color></size>";

        confirmRoot.SetActive(true);
        confirmRoot.transform.SetAsLastSibling();

        // 실수로 지우지 않게 처음엔 [취소]를 고른 상태로 연다.
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(confirmCancelButton.gameObject);
    }

    private void ConfirmDelete()
    {
        int slot = pendingDeleteSlot;
        CloseConfirm();
        if (slot < 0) return;

        SaveSlotService.Provider.DeleteSlot(slot);
        Refresh();
    }

    private void CloseConfirm()
    {
        pendingDeleteSlot = -1;
        if (confirmRoot != null) confirmRoot.SetActive(false);
        if (EventSystem.current != null && cards.Count > 0)
            EventSystem.current.SetSelectedGameObject(cards[Mathf.Clamp(current, 0, cards.Count - 1)].button.gameObject);
    }

    private void BuildConfirm()
    {
        confirmRoot = new GameObject("DeleteConfirm", typeof(RectTransform), typeof(Image));
        RectTransform cr = (RectTransform)confirmRoot.transform;
        cr.SetParent(transform, false);
        UnifiedStyle.Stretch(cr);
        Image dim = confirmRoot.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f);   // 뒤를 막고 어둡게

        RectTransform box = UnifiedStyle.NewRect("Box", cr);
        box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
        box.sizeDelta = new Vector2(680f, 320f);
        Image boxBg = box.gameObject.AddComponent<Image>();
        boxBg.color = new Color(UnifiedStyle.Panel.r, UnifiedStyle.Panel.g, UnifiedStyle.Panel.b, 0.98f);
        LatticeFrame.Attach(box, 320f, null, 10f, 0.7f);

        confirmText = UnifiedStyle.NewText("Message", box, font, "", 30f, UnifiedStyle.Text, TextAlignmentOptions.Center);
        RectTransform tr = confirmText.rectTransform;
        tr.anchorMin = new Vector2(0f, 0.38f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.offsetMin = new Vector2(40f, 0f);
        tr.offsetMax = new Vector2(-40f, -24f);
        confirmText.textWrappingMode = TextWrappingModes.Normal;

        confirmDeleteButton = MakeButton("Button_ConfirmDelete", box, "지우기", UnifiedStyle.ButtonKind.Danger, 220f, 60f);
        confirmCancelButton = MakeButton("Button_Cancel", box, "취소", UnifiedStyle.ButtonKind.Normal, 220f, 60f);
        foreach (Button b in new[] { confirmDeleteButton, confirmCancelButton })
        {
            RectTransform r = (RectTransform)b.transform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.pivot = new Vector2(0.5f, 0f);
        }
        ((RectTransform)confirmDeleteButton.transform).anchoredPosition = new Vector2(-130f, 36f);
        ((RectTransform)confirmCancelButton.transform).anchoredPosition = new Vector2(130f, 36f);
        confirmDeleteButton.onClick.AddListener(ConfirmDelete);
        confirmCancelButton.onClick.AddListener(CloseConfirm);

        // ←/→로 두 버튼 사이만 오가게
        Navigation nav = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = confirmCancelButton, selectOnLeft = confirmCancelButton };
        confirmDeleteButton.navigation = nav;
        nav.selectOnRight = confirmDeleteButton;
        nav.selectOnLeft = confirmDeleteButton;
        confirmCancelButton.navigation = nav;

        confirmRoot.SetActive(false);
    }

    // ── 도우미 ─────────────────────────────

    private Button MakeButton(string name, Transform parent, string label, UnifiedStyle.ButtonKind kind, float w, float h)
    {
        RectTransform rt = UnifiedStyle.NewRect(name, parent);
        rt.sizeDelta = new Vector2(w, h);
        Image bg = rt.gameObject.AddComponent<Image>();
        bg.raycastTarget = true;
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = bg;
        TextMeshProUGUI t = UnifiedStyle.NewText("Label", rt, font, label, 26f, UnifiedStyle.Text, TextAlignmentOptions.Center);
        UnifiedStyle.Stretch(t.rectTransform);
        UnifiedStyle.StyleButton(b, kind, font);
        return b;
    }

    /// <summary>타이틀 메뉴의 배경 그림 (TitleMenuPanel/Background).</summary>
    private Sprite FindTitleArt()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        foreach (Image img in canvas.rootCanvas.GetComponentsInChildren<Image>(true))
            if (img.name == "Background" && img.sprite != null && img.GetComponentInParent<TitleMenuPanel>(true) != null)
                return img.sprite;
        return null;
    }
}
