using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 저장 슬롯 한 칸. SaveSlotUIBuilder가 만드는 SaveSlotEntry 프리팹에 붙는다.
/// </summary>
public class SaveSlotEntry : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private UnityEngine.UI.Image thumbnailImage;
    [SerializeField] private TextMeshProUGUI slotNameText;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private TextMeshProUGUI savedAtText;
    [SerializeField] private UnityEngine.UI.Button button;

    private static readonly Color EmptyThumbnailColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    private Action<int> onClicked;
    private int slotIndex;

    /// <summary>슬롯 정보를 받아 한 칸을 그린다.</summary>
    public void Bind(SaveSlotInfo info, Action<int> clickHandler)
    {
        if (info == null) return;

        slotIndex = info.slotIndex;
        onClicked = clickHandler;

        if (numberText != null)
            numberText.text = (info.slotIndex + 1).ToString();

        if (thumbnailImage != null)
        {
            thumbnailImage.sprite = info.thumbnail;
            thumbnailImage.color = info.thumbnail != null ? Color.white : EmptyThumbnailColor;
        }

        if (info.isEmpty)
        {
            if (slotNameText != null) slotNameText.text = "비어 있음";
            if (detailText != null) detailText.text = "이 슬롯으로 새 게임을 시작합니다";
            if (savedAtText != null) savedAtText.text = "";
        }
        else
        {
            if (slotNameText != null)
                slotNameText.text = string.IsNullOrEmpty(info.slotName) ? $"슬롯 {info.slotIndex + 1}" : info.slotName;

            if (detailText != null)
                detailText.text = $"플레이 시간 {info.PlayTimeText}   Lv.{info.level}   {info.locationName}";

            if (savedAtText != null)
                savedAtText.text = info.SavedAtText;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        onClicked?.Invoke(slotIndex);
    }
}
