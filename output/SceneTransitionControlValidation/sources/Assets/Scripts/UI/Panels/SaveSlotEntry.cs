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

    private static readonly Color DeleteIdle = new Color(0.22f, 0.10f, 0.10f, 0.85f);
    private static readonly Color DeleteText = new Color(0.93f, 0.80f, 0.78f, 1f);

    private Action<int> onClicked;
    private Action<int> onDeleteClicked;
    private int slotIndex;
    private Button deleteButton;

    /// <summary>슬롯 정보를 받아 한 칸을 그린다. deleteHandler를 주면 저장된 슬롯에 [삭제] 버튼이 나온다.</summary>
    public void Bind(SaveSlotInfo info, Action<int> clickHandler, Action<int> deleteHandler = null)
    {
        if (info == null) return;

        slotIndex = info.slotIndex;
        onClicked = clickHandler;
        onDeleteClicked = deleteHandler;

        // [삭제] 버튼은 프리팹을 다시 만들지 않으려고 코드로 붙인다.
        if (deleteButton == null && deleteHandler != null) deleteButton = CreateDeleteButton();
        if (deleteButton != null) deleteButton.gameObject.SetActive(!info.isEmpty && deleteHandler != null);

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
                detailText.text = $"플레이 시간 {info.PlayTimeText}   {info.locationName}";

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

    private void HandleDeleteClick()
    {
        onDeleteClicked?.Invoke(slotIndex);
    }

    // 슬롯 오른쪽 위의 작은 [삭제] 버튼
    private Button CreateDeleteButton()
    {
        GameObject go = new GameObject("Button_Delete", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-18f, -14f);
        rt.sizeDelta = new Vector2(84f, 32f);

        Image bg = go.GetComponent<Image>();
        bg.color = DeleteIdle;

        Button b = go.GetComponent<Button>();
        b.targetGraphic = bg;
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.6f, 1.2f, 1.2f, 1f);
        colors.selectedColor = new Color(1.6f, 1.2f, 1.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.7f, 0.7f, 1f);
        colors.colorMultiplier = 1.5f;
        colors.fadeDuration = 0.08f;
        b.colors = colors;
        b.onClick.AddListener(HandleDeleteClick);

        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform lrt = label.GetComponent<RectTransform>();
        lrt.SetParent(rt, false);
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        if (slotNameText != null) text.font = slotNameText.font;
        text.text = "삭제";
        text.fontSize = 18f;
        text.color = DeleteText;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return b;
    }
}
