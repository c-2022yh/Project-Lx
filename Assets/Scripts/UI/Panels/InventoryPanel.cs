using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryPanel : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private Transform relicGrid;
    [SerializeField] private GameObject relicSlotPrefab;

    [Header("유물 데이터")]
    [SerializeField] private List<RelicData> ownedRelics;

    [Header("설명창 연결")]
    [SerializeField] private Image descIcon;
    [SerializeField] private TextMeshProUGUI descName;
    [SerializeField] private TextMeshProUGUI descText;

    [Header("장착/예산 연결")]
    [SerializeField] private Transform equipContainer;   // EquipSlotContainer
    [SerializeField] private TextMeshProUGUI budgetText; // BudgetText
    [SerializeField] private int maxBudget = 5;          // 총 예산

    private bool isBuilt = false;
    private RelicData selectedRelic;                  // 지금 선택한 유물
    private List<RelicData> equippedRelics = new List<RelicData>(); // 장착한 유물들

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        if (visible && !isBuilt)
        {
            BuildRelicGrid();
            UpdateBudgetText();
            isBuilt = true;
        }
    }

    private void BuildRelicGrid()
    {
        foreach (RelicData relic in ownedRelics)
        {
            GameObject slot = Instantiate(relicSlotPrefab, relicGrid);

            Transform iconTr = slot.transform.Find("Icon");
            if (iconTr != null)
            {
                Image iconImg = iconTr.GetComponent<Image>();
                iconImg.sprite = relic.icon;
                iconImg.enabled = true;
            }

            Transform costTr = slot.transform.Find("Cost");
            if (costTr != null)
                costTr.GetComponent<TextMeshProUGUI>().text = relic.cost.ToString();

            Button btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                RelicData captured = relic;
                btn.onClick.AddListener(() => ShowDescription(captured));
            }
        }
    }

    private void ShowDescription(RelicData relic)
    {
        selectedRelic = relic; // 선택한 유물 기억

        if (descIcon != null) { descIcon.sprite = relic.icon; descIcon.enabled = true; }
        if (descName != null) descName.text = relic.relicName;
        if (descText != null) descText.text = $"{relic.description}\n\n코스트: {relic.cost}";
    }

    // "장착" 버튼이 호출
    public void OnEquipButton()
    {
        if (selectedRelic == null) return;                  // 선택한 거 없으면 무시
        if (equippedRelics.Contains(selectedRelic)) return; // 이미 장착했으면 무시

        int currentCost = GetCurrentCost();
        if (currentCost + selectedRelic.cost > maxBudget)
        {
            Debug.Log("[인벤토리] 예산 초과! 장착 불가");
            return; // 예산 넘으면 장착 거부
        }

        equippedRelics.Add(selectedRelic);
        RefreshEquipSlots();
        UpdateBudgetText();
    }

    // "해제" 버튼이 호출
    public void OnUnequipButton()
    {
        if (selectedRelic == null) return;
        if (!equippedRelics.Contains(selectedRelic)) return;

        equippedRelics.Remove(selectedRelic);
        RefreshEquipSlots();
        UpdateBudgetText();
    }

    // 현재 사용 중인 코스트 합
    private int GetCurrentCost()
    {
        int sum = 0;
        foreach (RelicData r in equippedRelics) sum += r.cost;
        return sum;
    }

    // 예산 텍스트 갱신
    private void UpdateBudgetText()
    {
        if (budgetText != null)
            budgetText.text = $"코스트: {GetCurrentCost()} / {maxBudget}";
    }

    // 장착 슬롯 영역에 장착한 유물 아이콘 다시 그리기
    private void RefreshEquipSlots()
    {
        // 기존 표시 다 지우기
        foreach (Transform child in equipContainer)
            Destroy(child.gameObject);

        // 장착한 유물마다 아이콘 칸 만들기 (프리팹 재활용)
        foreach (RelicData relic in equippedRelics)
        {
            GameObject slot = Instantiate(relicSlotPrefab, equipContainer);

            Transform iconTr = slot.transform.Find("Icon");
            if (iconTr != null)
            {
                Image iconImg = iconTr.GetComponent<Image>();
                iconImg.sprite = relic.icon;
                iconImg.enabled = true;
            }

            Transform costTr = slot.transform.Find("Cost");
            if (costTr != null)
                costTr.GetComponent<TextMeshProUGUI>().text = relic.cost.ToString();
        }
    }
}