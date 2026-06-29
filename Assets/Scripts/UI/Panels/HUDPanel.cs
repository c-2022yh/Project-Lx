using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// HUD: 체력바, 5칸 기력 게이지, 스킬 슬롯.
/// UIManager의 이벤트를 구독해서 UI를 갱신.
/// </summary>
public class HUDPanel : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Image healthFillImage;  // HealthBar_Fill

    [Header("Soul Gauge (5칸)")]
    [SerializeField] private Image[] soulOrbs;       // SoulOrb_1 ~ 5

    [Header("Soul Orb Colors")]
    [SerializeField] private Color filledColor = new Color(80 / 255f, 180 / 255f, 255 / 255f, 1f);
    [SerializeField] private Color emptyColor = new Color(30 / 255f, 50 / 255f, 80 / 255f, 180 / 255f);

    [Header("Skill Slots")]
    [SerializeField] private SkillSlotUI[] skillSlots;

    void OnEnable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnHealthChanged -= UpdateHealth;
            UIManager.Instance.OnHealthChanged += UpdateHealth;
            UIManager.Instance.OnSoulChanged -= UpdateSoul;
            UIManager.Instance.OnSoulChanged += UpdateSoul;
        }
    }

    void OnDisable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnHealthChanged -= UpdateHealth;
            UIManager.Instance.OnSoulChanged -= UpdateSoul;
        }
    }

    void Start()
    {
        // UIManager.Start()가 먼저 호출됐을 수 있으므로 시작 시 한번 직접 갱신
        if (UIManager.Instance != null)
        {
            // 강제로 한번 표시 (이벤트가 이미 발행된 경우 대비)
            // 더 정교하게 하려면 UIManager에 GetCurrent... getter 추가
        }
    }

    //체력바 업데이트하기
    private void UpdateHealth(int current, int max)
    {
        if (healthFillImage != null)
            healthFillImage.fillAmount = max > 0 ? (float)current / max : 0f;
    }

    //기력 게이지 업데이트하기
    private void UpdateSoul(int current, int max)
    {
        if (soulOrbs == null) return;
        for (int i = 0; i < soulOrbs.Length; i++)
        {
            if (soulOrbs[i] == null) continue;
            soulOrbs[i].color = (i < current) ? filledColor : emptyColor;
        }
    }


    //스킬 쿨타임 돌리기
    public void StartSkillCooldown(int slotIndex, float cooldownTime)
    {
        if (skillSlots == null)
        {
            Debug.LogWarning("[HUDPanel] skillSlots 배열이 비어있음");
            return;
        }

        if (slotIndex < 0 || slotIndex >= skillSlots.Length)
        {
            Debug.LogWarning($"[HUDPanel] 잘못된 슬롯 인덱스: {slotIndex}");
            return;
        }

        SkillSlotUI slotUI = skillSlots[slotIndex];

        if (slotUI == null)
        {
            Debug.LogWarning($"[HUDPanel] {slotIndex}번 슬롯 UI가 연결되지 않음");
            return;
        }

        slotUI.StartCooldown(cooldownTime);
    }






}
