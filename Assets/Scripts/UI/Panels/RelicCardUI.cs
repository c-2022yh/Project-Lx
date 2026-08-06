using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//유물 선택 카드 UI
public class RelicCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text relicNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button selectButton;

    private RelicData currentRelic;


    //카드에 유물 정보 표시
    public void Setup(RelicData relicData, Action<RelicData> onSelected)
    {
        currentRelic = relicData;

        if (currentRelic == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        //아이콘
        if (iconImage != null)
        {
            bool hasIcon = currentRelic.Icon != null;

            iconImage.gameObject.SetActive(hasIcon);

            if (hasIcon)
            {
                iconImage.sprite = currentRelic.Icon;
            }
        }

        //유물 이름
        if (relicNameText != null)
        {
            relicNameText.text = currentRelic.RelicName;
        }

        //유물 설명
        if (descriptionText != null)
        {
            descriptionText.text = currentRelic.Description;
        }

        //카드 클릭 이벤트
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();

            selectButton.onClick.AddListener(() =>
            {
                onSelected?.Invoke(currentRelic);
            });
        }
    }
}