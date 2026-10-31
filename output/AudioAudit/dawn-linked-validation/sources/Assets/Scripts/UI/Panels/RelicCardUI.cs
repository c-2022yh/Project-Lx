using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//유물 선택 카드 UI
public class RelicCardUI : MonoBehaviour, IPointerEnterHandler
{
    //키보드로 고른 카드는 살짝 커진다 (게임이 멈춘 동안에도 움직이도록 실제 시간 사용)
    private const float HighlightScale = 1f;   //족자(RelicCardStyle)는 커지지 않고 살짝 내려온다
    private const float ScaleSpeed = 18f;

    private Vector3 baseScale = Vector3.one;
    private bool highlighted;
    private Action onHovered;

    public Button SelectButton => selectButton;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        highlighted = false;
        transform.localScale = baseScale;
    }

    private void Update()
    {
        Vector3 target = highlighted ? baseScale * HighlightScale : baseScale;
        float k = 1f - Mathf.Exp(-ScaleSpeed * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, target, k);
    }

    public void SetHighlighted(bool on)
    {
        highlighted = on;
        RelicCardStyle.For(this).SetSelected(on);
    }

    //마우스를 올려도 키보드 선택이 그 카드로 따라오게
    public void OnPointerEnter(PointerEventData eventData)
    {
        onHovered?.Invoke();
    }

    public void SetHoverCallback(Action callback)
    {
        onHovered = callback;
    }

    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text relicNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button selectButton;

    private RelicData currentRelic;
    private UnityEngine.Events.UnityAction selectAction;


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

        //10.28 자개 시안대로 카드 꾸미기 (배경색, 테두리, 글자 위치)
        RelicCardStyle.For(this).Show(currentRelic, iconImage, relicNameText, descriptionText);

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
            if (selectAction != null) selectButton.onClick.RemoveListener(selectAction);

            selectAction = () => onSelected?.Invoke(currentRelic);
            selectButton.onClick.AddListener(selectAction);
        }
    }
}