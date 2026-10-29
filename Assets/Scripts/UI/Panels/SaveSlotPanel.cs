using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 저장 슬롯 선택 화면.
/// 슬롯 정보는 SaveSlotService.Provider에서 가져오므로,
/// 세이브 시스템이 붙어도 이 스크립트는 고칠 게 없다.
/// </summary>
public class SaveSlotPanel : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotEntryPrefab;

    private readonly List<SaveSlotEntry> spawnedEntries = new();
    private TitleMenuPanel returnTo;

    // 삭제 확인 창 (코드로 만든다)
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
        if (slotContainer == null || slotEntryPrefab == null)
        {
            Debug.LogWarning("[SaveSlotPanel] slotContainer 또는 slotEntryPrefab이 연결되지 않았습니다.");
            return;
        }

        // 다시 열 때 슬롯이 중복 생성되지 않도록 먼저 비운다.
        foreach (SaveSlotEntry entry in spawnedEntries)
            if (entry != null) Destroy(entry.gameObject);

        spawnedEntries.Clear();

        ISaveSlotProvider provider = SaveSlotService.Provider;

        for (int i = 0; i < provider.SlotCount; i++)
        {
            GameObject go = Instantiate(slotEntryPrefab, slotContainer);
            go.SetActive(true);

            SaveSlotEntry entry = go.GetComponent<SaveSlotEntry>();
            if (entry == null)
            {
                Debug.LogWarning("[SaveSlotPanel] slotEntryPrefab에 SaveSlotEntry가 없습니다.");
                continue;
            }

            entry.Bind(provider.GetSlot(i), OnSlotClicked, OnDeleteClicked);
            spawnedEntries.Add(entry);
        }
    }

    private void OnSlotClicked(int slotIndex)
    {
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

        // [SAVE_HOOK] 슬롯이 다 찼을 때의 덮어쓰기 확인 팝업은 세이브 시스템과 같이 붙이면 된다.
        Debug.Log("[SaveSlotPanel] 빈 슬롯이 없습니다. 덮어쓸 슬롯을 직접 선택해주세요.");
    }

    /// <summary>"뒤로가기"</summary>
    public void OnBackButton()
    {
        if (IsConfirmOpen) CloseConfirm();
        SetVisible(false);
        returnTo?.SetVisible(true);
    }

    // ───────────── 슬롯 삭제 ─────────────

    private void OnDeleteClicked(int slotIndex)
    {
        SaveSlotInfo info = SaveSlotService.Provider.GetSlot(slotIndex);
        if (info == null || info.isEmpty) return;

        if (confirmRoot == null) BuildConfirm();

        pendingDeleteSlot = slotIndex;
        string where = string.IsNullOrEmpty(info.locationName) ? "" : $" · {info.locationName}";
        confirmText.text = $"슬롯 {slotIndex + 1}의 기록을 지울까요?\n" +
                           $"<size=70%><color=#9AA6AD>플레이 시간 {info.PlayTimeText}{where}\n지우면 되돌릴 수 없습니다.</color></size>";

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
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void Update()
    {
        if (!IsConfirmOpen) return;
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.xKey.wasPressedThisFrame)) CloseConfirm();
    }

    private void OnDisable()
    {
        if (IsConfirmOpen) CloseConfirm();
    }

    private void BuildConfirm()
    {
        TMP_FontAsset font = GetComponentInChildren<TextMeshProUGUI>(true)?.font;

        confirmRoot = new GameObject("DeleteConfirm", typeof(RectTransform), typeof(Image));
        RectTransform root = confirmRoot.GetComponent<RectTransform>();
        root.SetParent(transform, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        confirmRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);   // 뒤를 막고 어둡게

        RectTransform box = NewRect("Box", root);
        box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
        box.sizeDelta = new Vector2(620f, 300f);
        Image boxBg = box.gameObject.AddComponent<Image>();
        boxBg.color = new Color(0.06f, 0.08f, 0.10f, 0.98f);
        Outline outline = box.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.42f, 0.40f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform textRt = NewRect("Message", box);
        textRt.anchorMin = new Vector2(0f, 0.38f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.offsetMin = new Vector2(30f, 0f);
        textRt.offsetMax = new Vector2(-30f, -20f);
        confirmText = textRt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) confirmText.font = font;
        confirmText.fontSize = 28f;
        confirmText.alignment = TextAlignmentOptions.Center;
        confirmText.color = new Color(0.92f, 0.94f, 0.95f, 1f);
        confirmText.raycastTarget = false;

        confirmDeleteButton = NewButton("Button_ConfirmDelete", box, "지우기", font, new Vector2(-120f, 56f),
                                        new Color(0.45f, 0.16f, 0.15f, 1f));
        confirmCancelButton = NewButton("Button_Cancel", box, "취소", font, new Vector2(120f, 56f),
                                        new Color(0.14f, 0.19f, 0.22f, 1f));
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

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    private static Button NewButton(string name, Transform parent, string label, TMP_FontAsset font, Vector2 position, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(200f, 56f);

        Image bg = rt.gameObject.AddComponent<Image>();
        bg.color = color;
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = bg;
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f, 1f);
        colors.selectedColor = new Color(1.35f, 1.35f, 1.35f, 1f);
        colors.colorMultiplier = 1.4f;
        colors.fadeDuration = 0.08f;
        b.colors = colors;

        RectTransform textRt = NewRect("Label", rt);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;
        TextMeshProUGUI t = textRt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = label;
        t.fontSize = 24f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(0.94f, 0.95f, 0.96f, 1f);
        t.raycastTarget = false;
        return b;
    }
}
