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
    [SerializeField] private Image descIcon;       // Desc_Icon
    [SerializeField] private TextMeshProUGUI descName; // Desc_Name
    [SerializeField] private TextMeshProUGUI descText; // Desc_Text

    private bool isBuilt = false;

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        if (visible && !isBuilt)
        {
            BuildRelicGrid();
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
            {
                TextMeshProUGUI costText = costTr.GetComponent<TextMeshProUGUI>();
                costText.text = relic.cost.ToString();
            }

            // 칸 클릭하면 이 유물 설명 보여주기
            Button btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                RelicData captured = relic; // 클로저용 복사 (중요!)
                btn.onClick.AddListener(() => ShowDescription(captured));
            }
        }
    }

    // 유물 정보를 설명창에 채우기
    private void ShowDescription(RelicData relic)
    {
        if (descIcon != null)
        {
            descIcon.sprite = relic.icon;
            descIcon.enabled = true;
        }
        if (descName != null)
            descName.text = relic.relicName;
        if (descText != null)
            descText.text = $"{relic.description}\n\n코스트: {relic.cost}";
    }
}