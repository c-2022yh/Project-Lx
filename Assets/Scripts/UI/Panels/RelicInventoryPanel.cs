using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 유물 인벤토리. 왼쪽은 카테고리별 장착칸, 오른쪽은 보관함.
///
/// 장착 상태의 진짜 주인은 씬의 RelicManager다. 이 패널은 그걸 보여주고
/// EquipRelic/UnequipRelic을 호출할 뿐, 자체 목록을 따로 들고 있지 않다.
/// (기존 InventoryPanel이 자기만의 리스트를 갖고 있어서 게임과 따로 놀던 문제를 피한 것.)
///
/// 보유 목록은 RunState에서 읽는다. 이 게임은 유물을 먹으면 곧바로 장착되므로
/// '한 번이라도 장착했던 것'이 곧 보유한 것이고, 해제하면 보관함으로 내려온다.
///
/// 조작:
///   마우스 올리기  툴팁
///   좌클릭         선택 (아래 설명창에 표시)
///   우클릭         보관함이면 자동 장착, 장착칸이면 해제
///   드래그         원하는 칸으로 옮기기 / 보관함으로 빼면 해제
///   화살표 + Z     키보드로 같은 동작 (Z로 집고, 옮기고, Z로 놓기. X는 취소)
/// </summary>
public class RelicInventoryPanel : MonoBehaviour, IRelicSlotHost
{
    [Header("장착 슬롯")]
    [SerializeField] private Transform swordSlotContainer;
    [SerializeField] private Transform orbSlotContainer;
    [SerializeField] private Transform bodySlotContainer;
    [SerializeField] private TextMeshProUGUI bodyCostText;

    [Header("보관함")]
    [SerializeField] private Transform storageContainer;

    [Header("공용")]
    [SerializeField] private GameObject slotPrefab;

    [Header("설명창")]
    [SerializeField] private Image descIcon;
    [SerializeField] private TextMeshProUGUI descName;
    [SerializeField] private TextMeshProUGUI descMeta;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Button equipButton;
    [SerializeField] private TextMeshProUGUI equipButtonLabel;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("툴팁 / 드래그")]
    [SerializeField] private RelicTooltip tooltip;
    [SerializeField] private RelicDragLayer dragLayer;

    [Header("조작")]
    [Tooltip("화살표 키로 칸을 옮겨 다니는 기능을 켤지.")]
    [SerializeField] private bool keyboardNavigation = true;

    /// <summary>보관함 격자의 열 수. 빌더의 GridLayoutGroup.constraintCount와 맞춰야 한다.</summary>
    private const int StorageColumns = 6;

    private const int RowSword = 0;
    private const int RowOrb = 1;
    private const int RowBody = 2;
    private const int RowStorageStart = 3;

    private readonly List<RelicSlotView> spawnedSlots = new();

    private RelicManager relicManager;
    private RelicData selected;

    // 키보드 포커스. 칸은 Refresh마다 새로 만들어지므로 좌표로 기억했다가 다시 찾는다.
    private int focusRow = RowStorageStart;
    private int focusColumn;
    private bool hasFocus;

    // 키보드로 집어 든 유물 (Z로 집고 Z로 놓는 동안 들고 있는 것).
    private RelicData carried;

    // ── 열고 닫기 ──────────────────────────

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);

        if (!visible)
        {
            Unsubscribe();
            return;
        }

        relicManager = FindAnyObjectByType<RelicManager>();

        if (relicManager == null)
        {
            Debug.LogWarning("[RelicInventoryPanel] 씬에서 RelicManager를 찾지 못했습니다. 플레이어가 없는 씬인가요?");
        }
        else
        {
            // 이미 열린 패널에 SetVisible(true)를 또 부르면 OnDisable이 돌지 않아
            // 구독이 쌓인다. 먼저 떼고 붙인다 (없는 핸들러를 떼는 건 무해하다).
            relicManager.OnRelicsChanged -= Refresh;
            relicManager.OnRelicsChanged += Refresh;
        }

        selected = null;
        carried = null;
        hasFocus = false;

        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
        CancelCarry(false);
        EndDrag();

        if (tooltip != null) tooltip.Hide();
    }

    private void Unsubscribe()
    {
        if (relicManager == null) return;

        relicManager.OnRelicsChanged -= Refresh;
    }

    // ── 다시 그리기 ────────────────────────

    public void Refresh()
    {
        if (slotPrefab == null)
        {
            Debug.LogWarning("[RelicInventoryPanel] slotPrefab이 연결되지 않았습니다.");
            return;
        }

        ClearSlots();

        IReadOnlyList<RelicData> equipped = relicManager != null
            ? relicManager.EquippedRelics
            : new List<RelicData>();

        BuildEquipRow(swordSlotContainer, equipped, RelicCategory.Sword, RowSword);
        BuildEquipRow(orbSlotContainer, equipped, RelicCategory.Orb, RowOrb);
        BuildBodyRow(equipped);
        BuildStorage(equipped);

        UpdateCostText(equipped);
        UpdateDescription(equipped);

        RestoreFocus();
        RefreshCarriedVisual();
    }

    private void ClearSlots()
    {
        // 다시 열 때 칸이 중복 생성되지 않도록 먼저 비운다.
        foreach (RelicSlotView slot in spawnedSlots)
            if (slot != null) Destroy(slot.gameObject);

        spawnedSlots.Clear();
    }

    /// <summary>검 / 보주: 칸이 하나. 비어 있으면 빈 칸을 그린다.</summary>
    private void BuildEquipRow(Transform container, IReadOnlyList<RelicData> equipped,
                               RelicCategory category, int row)
    {
        if (container == null) return;

        int limit = RelicEquipRules.SlotLimitOf(category);
        List<RelicData> ofCategory = FilterByCategory(equipped, category);

        for (int i = 0; i < limit; i++)
            SpawnSlot(container, i < ofCategory.Count ? ofCategory[i] : null,
                      RelicSlotArea.Equip, category, row, i);
    }

    /// <summary>
    /// 신체: 개수 제한이 없고 코스트로만 막힌다.
    /// 장착한 것들 + 남은 코스트만큼의 빈 칸을 그려서 여유가 눈에 보이게 한다.
    /// </summary>
    private void BuildBodyRow(IReadOnlyList<RelicData> equipped)
    {
        if (bodySlotContainer == null) return;

        List<RelicData> body = FilterByCategory(equipped, RelicCategory.Body);
        int column = 0;

        foreach (RelicData relic in body)
        {
            SpawnSlot(bodySlotContainer, relic, RelicSlotArea.Equip, RelicCategory.Body, RowBody, column);
            column++;
        }

        int remaining = Mathf.Max(0, RelicEquipRules.RemainingBodyCost(equipped));

        // 코스트 1짜리가 최소 단위라 남은 코스트 = 더 낄 수 있는 최대 개수.
        for (int i = 0; i < remaining; i++)
        {
            SpawnSlot(bodySlotContainer, null, RelicSlotArea.Equip, RelicCategory.Body, RowBody, column);
            column++;
        }
    }

    /// <summary>보관함: 가지고 있지만 장착하지 않은 유물.</summary>
    private void BuildStorage(IReadOnlyList<RelicData> equipped)
    {
        if (storageContainer == null) return;

        int index = 0;

        foreach (string ownedId in RunState.Current.OwnedRelicIds)
        {
            RelicData relic = RelicDatabase.Get(ownedId);
            if (relic == null) continue;

            if (Contains(equipped, relic)) continue;

            SpawnSlot(storageContainer, relic, RelicSlotArea.Storage, relic.Category,
                      RowStorageStart + index / StorageColumns, index % StorageColumns);
            index++;
        }
    }

    private void SpawnSlot(Transform parent, RelicData relic, RelicSlotArea area,
                           RelicCategory category, int row, int column)
    {
        // UI는 worldPositionStays를 false로 넣어야 한다.
        // true(기본값)면 월드 스케일을 보존하려고 localScale을 멋대로 바꾼다.
        GameObject go = Instantiate(slotPrefab, parent, false);
        go.SetActive(true);

        RelicSlotView view = go.GetComponent<RelicSlotView>();

        if (view == null)
        {
            Debug.LogWarning("[RelicInventoryPanel] slotPrefab에 RelicSlotView가 없습니다.");
            return;
        }

        view.Bind(this, relic, area, category, row, column);
        view.SetSelected(relic != null && relic == selected);

        spawnedSlots.Add(view);
    }

    // ── 마우스: 툴팁 / 클릭 ────────────────

    public void OnSlotHoverEnter(RelicSlotView slot)
    {
        if (slot == null || slot.IsEmpty) return;
        if (tooltip != null) tooltip.Show(slot.Relic);
    }

    public void OnSlotHoverExit(RelicSlotView slot)
    {
        if (tooltip != null) tooltip.Hide();
    }

    public void OnSlotLeftClick(RelicSlotView slot)
    {
        if (slot == null) return;

        SetFocus(slot);

        if (slot.IsEmpty) return;

        Select(slot.Relic);
    }

    public void OnSlotRightClick(RelicSlotView slot)
    {
        if (slot == null || slot.IsEmpty) return;

        SetFocus(slot);
        Select(slot.Relic);

        if (slot.Area == RelicSlotArea.Equip) TryUnequip(slot.Relic);
        else TryEquip(slot.Relic);
    }

    // ── 마우스: 드래그 ─────────────────────

    public void OnSlotBeginDrag(RelicSlotView slot, PointerEventData eventData)
    {
        if (slot == null || slot.IsEmpty || dragLayer == null) return;

        // 키보드로 들고 있던 게 있으면 마우스 쪽을 우선한다.
        CancelCarry(false);

        if (tooltip != null) tooltip.Hide();

        Select(slot.Relic);
        dragLayer.Begin(slot, slot.Relic, eventData.position);
        slot.SetDraggingFrom(true);
    }

    public void OnSlotDrag(RelicSlotView slot, PointerEventData eventData)
    {
        if (dragLayer == null) return;

        dragLayer.Move(eventData.position);
    }

    public void OnSlotEndDrag(RelicSlotView slot, PointerEventData eventData)
    {
        // OnDrop이 먼저 불리고 여기가 나중이다.
        // 아무 데도 놓지 않았을 때를 위한 뒷정리.
        EndDrag();
    }

    public void OnSlotDrop(RelicSlotView target, PointerEventData eventData)
    {
        if (target == null) return;

        if (target.Area == RelicSlotArea.Storage) DropToStorage();
        else DropToCategory(target.Category);
    }

    public void OnZoneDrop(RelicDropZone zone, PointerEventData eventData)
    {
        if (zone == null) return;

        if (zone.IsStorage) DropToStorage();
        else DropToCategory(zone.Category);
    }

    /// <summary>드래그 중인(또는 키보드로 든) 유물을 장착 줄에 놓았을 때.</summary>
    private void DropToCategory(RelicCategory category)
    {
        RelicData relic = TakeHeldRelic();
        if (relic == null) return;

        if (relic.Category != category)
        {
            SetHint($"{RelicEquipRules.LabelOf(relic.Category)} 유물은 {RelicEquipRules.LabelOf(category)} 칸에 넣을 수 없습니다");
            return;
        }

        TryEquip(relic);
    }

    /// <summary>보관함 쪽에 놓았을 때. 장착 중이던 유물이면 해제된다.</summary>
    private void DropToStorage()
    {
        RelicData relic = TakeHeldRelic();
        if (relic == null) return;

        if (!IsEquipped(relic))
        {
            SetHint($"{relic.RelicName} 은(는) 이미 보관함에 있습니다");
            return;
        }

        TryUnequip(relic);
    }

    /// <summary>
    /// 지금 들고 있는 유물을 꺼내고 드래그/집기 상태를 끝낸다.
    /// 장착 처리가 Refresh를 부르면서 칸들을 지워버리기 때문에,
    /// 무엇을 할지 정하기 전에 먼저 상태를 정리해야 고스트 아이콘이 남지 않는다.
    /// </summary>
    private RelicData TakeHeldRelic()
    {
        RelicData relic = null;

        if (dragLayer != null && dragLayer.IsDragging) relic = dragLayer.DraggedRelic;
        else if (carried != null) relic = carried;

        EndDrag();
        CancelCarry(false);

        return relic;
    }

    private void EndDrag()
    {
        if (dragLayer != null) dragLayer.End();

        // 드롭이 먼저 처리되면 SourceSlot이 이미 비워져 있다.
        // 개별 칸을 되돌리는 대신 전체를 다시 칠해서 흐린 칸이 남지 않게 한다.
        RefreshCarriedVisual();
    }

    // ── 장착 / 해제 ────────────────────────

    /// <summary>설명창의 장착/해제 버튼.</summary>
    public void OnEquipButton()
    {
        if (selected == null) return;

        if (IsEquipped(selected)) TryUnequip(selected);
        else TryEquip(selected);
    }

    private void TryEquip(RelicData relic)
    {
        if (relic == null) return;

        if (relicManager == null)
        {
            SetHint("RelicManager가 없어 장착할 수 없습니다");
            return;
        }

        IReadOnlyList<RelicData> equipped = relicManager.EquippedRelics;

        if (!RelicEquipRules.CanEquip(relic, equipped, out string reason, out RelicData replaces))
        {
            SetHint(reason);
            return;
        }

        // 검/보주는 칸이 하나뿐이라 기존 것을 먼저 벗긴다.
        // EquipRelic은 효과가 하나도 만들어지지 않으면 false를 돌려주므로,
        // 실패하면 벗겼던 것을 되돌려야 칸이 빈 채로 남지 않는다.
        if (replaces != null)
        {
            relicManager.UnequipRelic(replaces);

            if (!relicManager.EquipRelic(relic))
            {
                relicManager.EquipRelic(replaces);
                SetHint($"{relic.RelicName} 장착 실패 (효과가 없는 유물)");
                return;
            }

            SetHint($"{replaces.RelicName} → {relic.RelicName} 교체됨");
            return;
        }

        if (!relicManager.EquipRelic(relic))
        {
            SetHint($"{relic.RelicName} 장착 실패 (효과가 없는 유물)");
            return;
        }

        SetHint($"{relic.RelicName} 장착됨");
    }

    private void TryUnequip(RelicData relic)
    {
        if (relic == null) return;

        if (relicManager == null)
        {
            SetHint("RelicManager가 없어 해제할 수 없습니다");
            return;
        }

        if (!IsEquipped(relic))
        {
            SetHint($"{relic.RelicName} 은(는) 장착 중이 아닙니다");
            return;
        }

        relicManager.UnequipRelic(relic);
        SetHint($"{relic.RelicName} 해제됨");
    }

    private void Select(RelicData relic)
    {
        selected = relic;

        foreach (RelicSlotView s in spawnedSlots)
            if (s != null) s.SetSelected(s.Relic != null && s.Relic == selected);

        UpdateDescription(CurrentEquipped());
    }

    // ── 키보드 ─────────────────────────────

    private void Update()
    {
        if (!keyboardNavigation) return;
        if (dragLayer != null && dragLayer.IsDragging) return;

        ReadNavigationInput();
    }

    private void ReadNavigationInput()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.leftArrowKey.wasPressedThisFrame) MoveFocus(0, -1);
        if (kb.rightArrowKey.wasPressedThisFrame) MoveFocus(0, 1);
        if (kb.upArrowKey.wasPressedThisFrame) MoveFocus(-1, 0);
        if (kb.downArrowKey.wasPressedThisFrame) MoveFocus(1, 0);

        if (kb.zKey.wasPressedThisFrame) ConfirmKey();
        if (kb.xKey.wasPressedThisFrame) CancelCarry(true);
#else
        if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveFocus(0, -1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) MoveFocus(0, 1);
        if (Input.GetKeyDown(KeyCode.UpArrow)) MoveFocus(-1, 0);
        if (Input.GetKeyDown(KeyCode.DownArrow)) MoveFocus(1, 0);

        if (Input.GetKeyDown(KeyCode.Z)) ConfirmKey();
        if (Input.GetKeyDown(KeyCode.X)) CancelCarry(true);
#endif
    }

    /// <summary>Z: 들고 있는 게 없으면 집고, 있으면 지금 칸에 놓는다.</summary>
    private void ConfirmKey()
    {
        RelicSlotView slot = FocusedSlot();

        if (slot == null)
        {
            // 아직 한 번도 움직이지 않았으면 첫 칸을 잡아준다.
            RestoreFocus();
            return;
        }

        if (carried == null)
        {
            if (slot.IsEmpty) return;

            carried = slot.Relic;
            Select(carried);
            RefreshCarriedVisual();
            SetHint($"{carried.RelicName} 을(를) 들었습니다. 화살표로 옮기고 Z로 놓기 (X 취소)");
            return;
        }

        if (slot.Area == RelicSlotArea.Storage) DropToStorage();
        else DropToCategory(slot.Category);
    }

    /// <summary>X: 집은 것을 내려놓는다.</summary>
    private void CancelCarry(bool announce)
    {
        if (carried == null) return;

        string name = carried.RelicName;
        carried = null;

        RefreshCarriedVisual();

        if (announce) SetHint($"{name} 내려놓음");
    }

    /// <summary>들고 있는 유물이 있던 칸을 흐리게 표시한다.</summary>
    private void RefreshCarriedVisual()
    {
        foreach (RelicSlotView s in spawnedSlots)
            if (s != null) s.SetDraggingFrom(carried != null && s.Relic == carried);
    }

    private void MoveFocus(int rowDelta, int columnDelta)
    {
        if (spawnedSlots.Count == 0) return;

        if (!hasFocus)
        {
            RestoreFocus();
            return;
        }

        if (rowDelta != 0)
        {
            int row = FindRow(focusRow, rowDelta);
            if (row < 0) return;

            focusRow = row;
            focusColumn = ClampColumn(row, focusColumn);
        }
        else
        {
            List<RelicSlotView> row = SlotsInRow(focusRow);
            if (row.Count == 0) return;

            int index = Mathf.Clamp(focusColumn + columnDelta, 0, row.Count - 1);
            focusColumn = row[index].Column;
        }

        ApplyFocus();
    }

    /// <summary>direction 방향으로 칸이 있는 가장 가까운 줄을 찾는다. 없으면 -1.</summary>
    private int FindRow(int from, int direction)
    {
        int lowest = int.MaxValue;
        int highest = int.MinValue;

        foreach (RelicSlotView s in spawnedSlots)
        {
            if (s == null) continue;
            if (s.Row < lowest) lowest = s.Row;
            if (s.Row > highest) highest = s.Row;
        }

        if (lowest > highest) return -1;

        for (int row = from + direction; row >= lowest && row <= highest; row += direction)
            if (SlotsInRow(row).Count > 0) return row;

        return -1;
    }

    private int ClampColumn(int row, int column)
    {
        List<RelicSlotView> slots = SlotsInRow(row);
        if (slots.Count == 0) return 0;

        int best = slots[0].Column;
        int bestDistance = Mathf.Abs(best - column);

        foreach (RelicSlotView s in slots)
        {
            int distance = Mathf.Abs(s.Column - column);
            if (distance >= bestDistance) continue;

            best = s.Column;
            bestDistance = distance;
        }

        return best;
    }

    private List<RelicSlotView> SlotsInRow(int row)
    {
        List<RelicSlotView> result = new List<RelicSlotView>();

        foreach (RelicSlotView s in spawnedSlots)
            if (s != null && s.Row == row) result.Add(s);

        result.Sort((a, b) => a.Column.CompareTo(b.Column));
        return result;
    }

    private RelicSlotView FocusedSlot()
    {
        if (!hasFocus) return null;

        foreach (RelicSlotView s in spawnedSlots)
            if (s != null && s.Row == focusRow && s.Column == focusColumn) return s;

        return null;
    }

    private void SetFocus(RelicSlotView slot)
    {
        if (slot == null) return;

        focusRow = slot.Row;
        focusColumn = slot.Column;
        hasFocus = true;

        ApplyFocus();
    }

    /// <summary>Refresh로 칸이 새로 만들어진 뒤 포커스를 같은 자리에 되돌린다.</summary>
    private void RestoreFocus()
    {
        if (spawnedSlots.Count == 0)
        {
            hasFocus = false;
            return;
        }

        if (SlotsInRow(focusRow).Count == 0)
        {
            int row = FindRow(focusRow, 1);
            if (row < 0) row = FindRow(focusRow, -1);
            if (row < 0) { hasFocus = false; return; }

            focusRow = row;
        }

        focusColumn = ClampColumn(focusRow, focusColumn);
        hasFocus = true;

        ApplyFocus();
    }

    private void ApplyFocus()
    {
        RelicSlotView focused = null;

        foreach (RelicSlotView s in spawnedSlots)
        {
            if (s == null) continue;

            bool isFocused = hasFocus && s.Row == focusRow && s.Column == focusColumn;
            s.SetFocused(isFocused);

            if (isFocused) focused = s;
        }

        // 키보드로 옮겨 다닐 때도 툴팁이 따라오게 한다.
        if (tooltip == null) return;

        if (focused != null && !focused.IsEmpty) tooltip.Show(focused.Relic);
        else tooltip.Hide();
    }

    // ── 표시 갱신 ──────────────────────────

    private void UpdateCostText(IReadOnlyList<RelicData> equipped)
    {
        if (bodyCostText == null) return;

        int used = RelicEquipRules.UsedBodyCost(equipped);
        bodyCostText.text = $"코스트  {used} / {RelicEquipRules.BodyCostBudget}";
    }

    private void UpdateDescription(IReadOnlyList<RelicData> equipped)
    {
        bool has = selected != null;

        if (descIcon != null)
        {
            descIcon.sprite = has ? selected.Icon : null;
            descIcon.enabled = has && selected.Icon != null;
        }

        if (descName != null)
            descName.text = has ? selected.RelicName : "유물을 선택하세요";

        if (descMeta != null)
        {
            if (!has) descMeta.text = "";
            else if (selected.Category == RelicCategory.Body)
                descMeta.text = $"{RelicEquipRules.LabelOf(selected.Category)}   ·   코스트 {selected.Cost}";
            else
                descMeta.text = RelicEquipRules.LabelOf(selected.Category);
        }

        if (descText != null)
            descText.text = has ? selected.Description : "";

        if (equipButton != null)
            equipButton.interactable = has && relicManager != null;

        if (equipButtonLabel != null)
            equipButtonLabel.text = has && Contains(equipped, selected) ? "해제" : "장착";
    }

    private void SetHint(string message)
    {
        if (hintText != null) hintText.text = message;
        Debug.Log("[RelicInventoryPanel] " + message);
    }

    // ── 보조 ───────────────────────────────

    private IReadOnlyList<RelicData> CurrentEquipped()
    {
        return relicManager != null ? relicManager.EquippedRelics : new List<RelicData>();
    }

    private bool IsEquipped(RelicData relic)
    {
        return Contains(CurrentEquipped(), relic);
    }

    private static List<RelicData> FilterByCategory(IReadOnlyList<RelicData> source, RelicCategory category)
    {
        List<RelicData> result = new List<RelicData>();

        for (int i = 0; i < source.Count; i++)
            if (source[i] != null && source[i].Category == category) result.Add(source[i]);

        return result;
    }

    private static bool Contains(IReadOnlyList<RelicData> list, RelicData relic)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] == relic) return true;

        return false;
    }
}
