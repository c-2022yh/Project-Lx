using UnityEngine;
using UnityEngine.UI;

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

    private PlayerSkill playerSkill;

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
        UnsubscribeSkills();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnHealthChanged -= UpdateHealth;
            UIManager.Instance.OnSoulChanged -= UpdateSoul;
        }
    }

    void Start()
    {
        ConnectPlayerSkills();
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


    // 플레이어가 늦게 생성되거나 HUD가 다시 켜져도 실제 상태로 복구한다.
    private void LateUpdate()
    {
        if (playerSkill == null) ConnectPlayerSkills();
        RefreshCooldowns();
    }

    private void ConnectPlayerSkills()
    {
        UnsubscribeSkills();
        playerSkill = FindAnyObjectByType<PlayerSkill>();
        if (playerSkill != null) playerSkill.OnSkillsChanged += RefreshSkills;
        RefreshSkills();
    }

    private void UnsubscribeSkills()
    {
        if (playerSkill != null) playerSkill.OnSkillsChanged -= RefreshSkills;
        playerSkill = null;
    }

    private void RefreshSkills()
    {
        if (skillSlots == null) return;
        foreach (SkillSlotUI slot in skillSlots)
        {
            if (slot == null) continue;
            slot.BindSkill(playerSkill != null
                ? playerSkill.GetEquippedSkill((int)slot.SlotType) : null);
        }
        RefreshCooldowns();
    }

    private void RefreshCooldowns()
    {
        if (playerSkill == null || skillSlots == null) return;
        foreach (SkillSlotUI slot in skillSlots)
        {
            if (slot == null) continue;
            SkillData skill = playerSkill.GetEquippedSkill((int)slot.SlotType);
            slot.SetCooldown(playerSkill.GetCooldownRemaining(skill),
                playerSkill.GetCooldownDuration(skill));
        }
    }





}
