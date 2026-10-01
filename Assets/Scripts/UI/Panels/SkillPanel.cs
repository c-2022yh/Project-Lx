using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 스킬 창. 위는 장착칸 다섯 개(X / A / S / D / F), 아래는 보유 스킬.
///
/// 자체 목록을 들고 있지 않고 PlayerSkill을 그대로 읽고 쓴다.
/// (기존 InventoryPanel이 자기만의 리스트를 갖고 있어서 게임과 따로 놀던 문제를 피한 것.)
///
/// 【X칸은 잠겨 있다】
/// PlayerSkill에서 0번은 ExclusiveSlot이고, 검 유물이 SetExclusiveSkill로만 채운다.
/// CanEquipSkill과 UnequipSkill이 X의 수동 변경·해제를 거절하므로
/// 여기서도 드래그를 시작하지 않고 드롭도 받지 않는다.
/// 지금 X에 뭐가 들어 있는지는 보여줘야 하니 칸 자체는 그린다.
///
/// 【보유 목록】
/// PlayerSkill.OwnedSkills를 읽는다. 유물이 GrantSkill로 스킬을 주면
/// A→S→D→F 첫 빈칸에 자동으로 들어가고, 칸이 꽉 차 있으면 보유만 된다.
/// 그렇게 남은 것들이 아래 목록에 뜬다.
///
/// 조작:
///   마우스 올리기  아래 설명창에 표시
///   좌클릭         선택 (설명창 고정)
///   우클릭         장착칸이면 해제 / 보유 목록이면 빈 칸에 장착
///   드래그         칸끼리 자리 바꾸기, 보유 목록으로 끌어내면 해제
/// </summary>
public class SkillPanel : MonoBehaviour, ISkillSlotHost
{
    /// <summary>칸 순서는 PlayerSkill의 슬롯 번호 그대로다. 0=X, 1=A, 2=S, 3=D, 4=F.</summary>
    private static readonly string[] SlotKeyLabels = { "X", "A", "S", "D", "F" };

    [Header("연결")]
    [SerializeField] private Transform equipSlotContainer;
    [SerializeField] private Transform storageContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private SkillDragLayer dragLayer;

    [Tooltip("보유 목록이 비었을 때 대신 띄울 안내문.")]
    [SerializeField] private GameObject storageEmptyNotice;

    [Header("설명창")]
    [SerializeField] private Image descIcon;
    [SerializeField] private TextMeshProUGUI descName;
    [SerializeField] private TextMeshProUGUI descMeta;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("표시")]
    [Tooltip("X칸을 잠긴 채로 보여준다. 끄면 A/S/D/F 네 칸만 그린다.")]
    [SerializeField] private bool showExclusiveSlot = true;

    private readonly List<SkillPanelSlot> spawnedSlots = new List<SkillPanelSlot>();

    private PlayerSkill playerSkill;
    private SkillData selected;

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);

        if (!visible) return;

        playerSkill = FindAnyObjectByType<PlayerSkill>();

        if (playerSkill == null)
        {
            Debug.LogWarning("[SkillPanel] 씬에서 PlayerSkill을 찾지 못했습니다. 플레이어가 없는 씬인가요?");
        }
        else
        {
            // 이미 열린 패널에 SetVisible(true)를 또 부르면 OnDisable이 돌지 않아
            // 구독이 쌓인다. 먼저 떼고 붙인다 (없는 핸들러를 떼는 건 무해하다).
            playerSkill.OnSkillsChanged -= Refresh;
            playerSkill.OnSkillsChanged += Refresh;
        }

        selected = null;
        Refresh();
    }

    private void OnDisable()
    {
        EndDrag();
        Unsubscribe();
        ClearSlots();
    }

    private void Unsubscribe()
    {
        if (playerSkill == null) return;

        playerSkill.OnSkillsChanged -= Refresh;
    }

    // ── 화면 그리기 ─────────────────────────

    public void Refresh()
    {
        ClearSlots();
        BuildEquipRow();
        BuildStorage();
        UpdateDescription(selected);

        SetHint(playerSkill == null ? "PlayerSkill을 찾지 못했습니다" : "");
    }

    private void ClearSlots()
    {
        foreach (SkillPanelSlot slot in spawnedSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }

        spawnedSlots.Clear();
    }

    private void BuildEquipRow()
    {
        if (equipSlotContainer == null) return;

        int first = showExclusiveSlot ? 0 : PlayerSkill.FirstNormalSlot;

        for (int i = first; i < PlayerSkill.SlotCount; i++)
        {
            bool locked = i == PlayerSkill.ExclusiveSlot;

            SpawnSlot(equipSlotContainer, EquippedAt(i), SkillSlotArea.Equip,
                      i, SlotKeyLabels[i], locked);
        }
    }

    /// <summary>보유 목록: 가지고 있지만 지금 칸에 들어가 있지 않은 일반 스킬.</summary>
    private void BuildStorage()
    {
        int shown = 0;

        if (storageContainer != null && playerSkill != null)
        {
            foreach (SkillData skill in playerSkill.OwnedSkills)
            {
                if (skill == null) continue;
                if (playerSkill.IsSkillEquipped(skill)) continue;

                SpawnSlot(storageContainer, skill, SkillSlotArea.Storage, -1, "", false);
                shown++;
            }
        }

        // 이 목록이 비어 있는 건 대체로 정상이다. 지금 게임에서 일반 스킬을 주는
        // 유물은 그림자 하나뿐이고, PlayerSkill.GrantSkill이 받는 즉시 A~F의 첫
        // 빈칸에 자동 장착해버린다. 즉 "보유했지만 안 낀 스킬"이 생길 일이 거의 없다.
        // 아무 말도 없으면 창이 고장난 것처럼 보이므로 안내를 띄운다.
        if (storageEmptyNotice != null) storageEmptyNotice.SetActive(shown == 0);
    }

    private void SpawnSlot(Transform parent, SkillData skill, SkillSlotArea area,
                           int slotIndex, string keyLabel, bool locked)
    {
        if (slotPrefab == null) return;

        // UI는 worldPositionStays를 false로 넣어야 한다.
        // true(기본값)면 월드 스케일을 보존하려고 localScale을 멋대로 바꾼다.
        GameObject go = Instantiate(slotPrefab, parent, false);
        go.SetActive(true);

        SkillPanelSlot view = go.GetComponent<SkillPanelSlot>();

        if (view == null)
        {
            Debug.LogWarning("[SkillPanel] slotPrefab에 SkillPanelSlot이 없습니다.");
            return;
        }

        view.Bind(this, skill, area, slotIndex, keyLabel, locked);
        view.SetSelected(skill != null && skill == selected);

        spawnedSlots.Add(view);
    }

    // ── 입력 ───────────────────────────────

    public void OnSkillSlotHoverEnter(SkillPanelSlot slot)
    {
        if (slot == null) return;

        if (slot.IsLocked) SetHint("X 스킬은 검 유물로만 바뀝니다");

        if (slot.IsEmpty) return;
        if (selected != null) return;   // 고른 게 있으면 그걸 계속 보여준다

        UpdateDescription(slot.Skill);
    }

    public void OnSkillSlotHoverExit(SkillPanelSlot slot)
    {
        if (slot != null && slot.IsLocked) SetHint("");

        if (selected != null) return;

        UpdateDescription(null);
    }

    public void OnSkillSlotLeftClick(SkillPanelSlot slot)
    {
        if (slot == null) return;

        selected = slot.IsEmpty ? null : slot.Skill;

        foreach (SkillPanelSlot other in spawnedSlots)
        {
            if (other != null) other.SetSelected(!other.IsEmpty && other.Skill == selected);
        }

        UpdateDescription(selected);
    }

    public void OnSkillSlotRightClick(SkillPanelSlot slot)
    {
        if (slot == null || slot.IsEmpty) return;

        if (slot.IsLocked)
        {
            SetHint("X 스킬은 검 유물로만 바뀝니다");
            return;
        }

        if (playerSkill == null)
        {
            SetHint("PlayerSkill을 찾지 못했습니다");
            return;
        }

        if (slot.Area == SkillSlotArea.Equip)
        {
            if (playerSkill.UnequipSkill(slot.SlotIndex)) SetHint($"{NameOf(slot.Skill)} 해제됨");
            else SetHint("지금은 해제할 수 없습니다");

            return;
        }

        int empty = FirstEmptyNormalSlot();

        if (empty < 0)
        {
            SetHint("빈 칸이 없습니다. 먼저 하나를 해제해주세요");
            return;
        }

        TryEquip(empty, slot.Skill);
    }

    // ── 드래그 ─────────────────────────────

    public void OnSkillSlotBeginDrag(SkillPanelSlot slot, PointerEventData eventData)
    {
        if (dragLayer == null || slot == null || slot.IsEmpty || slot.IsLocked) return;

        dragLayer.Begin(slot, slot.Skill, eventData.position);
        slot.SetDraggingFrom(true);
    }

    public void OnSkillSlotDrag(SkillPanelSlot slot, PointerEventData eventData)
    {
        if (dragLayer == null) return;

        dragLayer.Move(eventData.position);
    }

    public void OnSkillSlotEndDrag(SkillPanelSlot slot, PointerEventData eventData)
    {
        // 드롭이 먼저 처리됐으면 이미 끝나 있다. 여기 남는 건 허공에 놓은 경우다.
        EndDrag();
    }

    public void OnSkillSlotDrop(SkillPanelSlot target, PointerEventData eventData)
    {
        if (dragLayer == null || !dragLayer.IsDragging) return;
        if (target == null) return;

        SkillPanelSlot source = dragLayer.SourceSlot;
        SkillData skill = dragLayer.DraggedSkill;

        // 장착이 바뀌면 OnSkillsChanged로 Refresh가 돌아 칸이 전부 지워진다.
        // 무엇을 할지 정하기 전에 드래그부터 끝내야 지워진 칸을 가리키는 고스트가 남지 않는다.
        EndDrag();

        if (source == null || skill == null) return;
        if (source == target) return;

        if (playerSkill == null)
        {
            SetHint("PlayerSkill을 찾지 못했습니다");
            return;
        }

        if (target.IsLocked)
        {
            SetHint("X 스킬은 검 유물로만 바뀝니다");
            return;
        }

        if (target.Area == SkillSlotArea.Storage)
        {
            Unequip(source, skill);
            return;
        }

        if (source.Area == SkillSlotArea.Equip) SwapSlots(source.SlotIndex, target.SlotIndex);
        else TryEquip(target.SlotIndex, skill);
    }

    /// <summary>
    /// 보유 목록의 빈 곳에 놓았을 때. 장착칸에서 끌어냈으면 해제한다.
    /// 칸 위에 놓은 경우는 OnSkillSlotDrop이 먼저 처리하므로 여기까지 오지 않는다.
    /// </summary>
    public void OnStorageDrop(PointerEventData eventData)
    {
        if (dragLayer == null || !dragLayer.IsDragging) return;

        SkillPanelSlot source = dragLayer.SourceSlot;
        SkillData skill = dragLayer.DraggedSkill;

        EndDrag();

        if (source == null || skill == null) return;

        if (playerSkill == null)
        {
            SetHint("PlayerSkill을 찾지 못했습니다");
            return;
        }

        Unequip(source, skill);
    }

    private void EndDrag()
    {
        if (dragLayer == null) return;

        SkillPanelSlot source = dragLayer.SourceSlot;
        if (source != null) source.SetDraggingFrom(false);

        dragLayer.End();
    }

    // ── PlayerSkill 조작 ────────────────────

    private void Unequip(SkillPanelSlot source, SkillData skill)
    {
        if (source.Area != SkillSlotArea.Equip) return;

        if (playerSkill.UnequipSkill(source.SlotIndex)) SetHint($"{NameOf(skill)} 해제됨");
        else SetHint("지금은 해제할 수 없습니다");
    }

    /// <summary>
    /// 칸에 스킬을 넣는다. 그 칸이 차 있으면 먼저 비운다.
    /// EquipSkill은 빈 칸에만 넣어주기 때문이다.
    /// 막히면 PlayerSkill이 돌려준 이유를 그대로 보여준다.
    /// </summary>
    private void TryEquip(int slotIndex, SkillData skill)
    {
        SkillData current = EquippedAt(slotIndex);

        if (current == skill) return;
        if (current != null) playerSkill.UnequipSkill(slotIndex);

        if (playerSkill.EquipSkill(slotIndex, skill))
        {
            SetHint($"{NameOf(skill)} → {SlotKeyLabels[slotIndex]} 칸에 장착");
            return;
        }

        playerSkill.CanEquipSkill(slotIndex, skill, out string reason);
        SetHint(reason ?? "장착하지 못했습니다");

        // 비워만 놓고 끝나면 칸을 잃는다. 원래 있던 것을 되돌린다.
        if (current != null) playerSkill.EquipSkill(slotIndex, current);
    }

    /// <summary>
    /// 두 칸의 스킬을 맞바꾼다.
    /// EquipSkill은 빈 칸에만 넣어주므로 둘 다 비운 다음에 넣어야 한다.
    /// </summary>
    private void SwapSlots(int a, int b)
    {
        if (a == b) return;

        SkillData first = EquippedAt(a);
        SkillData second = EquippedAt(b);

        if (first != null) playerSkill.UnequipSkill(a);
        if (second != null) playerSkill.UnequipSkill(b);

        if (second != null) playerSkill.EquipSkill(a, second);
        if (first != null) playerSkill.EquipSkill(b, first);

        SetHint($"{SlotKeyLabels[a]} ↔ {SlotKeyLabels[b]}");
    }

    private SkillData EquippedAt(int index)
    {
        if (playerSkill == null) return null;

        return playerSkill.GetEquippedSkill(index);
    }

    /// <summary>X칸을 뺀 A/S/D/F 중 첫 빈 칸.</summary>
    private int FirstEmptyNormalSlot()
    {
        for (int i = PlayerSkill.FirstNormalSlot; i < PlayerSkill.SlotCount; i++)
        {
            if (EquippedAt(i) == null) return i;
        }

        return -1;
    }

    // ── 설명창 ─────────────────────────────

    private void UpdateDescription(SkillData skill)
    {
        bool has = skill != null;

        if (descIcon != null)
        {
            descIcon.sprite = has ? skill.icon : null;
            descIcon.enabled = has && skill.icon != null;
        }

        if (descName != null) descName.text = has ? NameOf(skill) : "스킬을 선택하세요";

        if (descMeta != null)
        {
            descMeta.text = has
                ? $"쿨타임 {skill.cooldownTime:0.#}초     소모 기력 {skill.soulCost}"
                : "";
        }

        if (descText != null) descText.text = has ? skill.description : "";
    }

    private void SetHint(string message)
    {
        if (hintText != null) hintText.text = message;
    }

    /// <summary>이름이 비어 있는 에셋도 있어서 파일 이름으로 대신한다.</summary>
    private static string NameOf(SkillData skill)
    {
        if (skill == null) return "";

        return string.IsNullOrEmpty(skill.skillName) ? skill.name : skill.skillName;
    }
}
