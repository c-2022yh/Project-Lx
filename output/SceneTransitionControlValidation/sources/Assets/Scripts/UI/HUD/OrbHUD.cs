using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

//플레이어의 현재 기력에 따라 보주 HUD를 갱신하는 스크립트
public partial class OrbHUD : MonoBehaviour
{
    private PlayerEnergy playerEnergy;
    private PlayerRelicManager relicManager;
    private PlayerAwakening playerAwakening;
    private PlayerSkill playerSkill;

    [Header("Orb Design")]
    [SerializeField] private Image emptyImage;
    [SerializeField] private Image fullEffectImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private Image cloudImage;
    [SerializeField] private Image backdropImage;

    [Header("Effect Layers")]
    [Tooltip("Shared rear layer. Created at runtime if unassigned; never contains relic-specific assets in the HUD prefab.")]
    [SerializeField] private RectTransform backEffectRoot;
    [FormerlySerializedAs("effectRoot")]
    [Tooltip("Shared front layer. Created at runtime if unassigned.")]
    [SerializeField] private RectTransform frontEffectRoot;

    [Header("Energy Fill")]
    [SerializeField] private Image orbImageFill;

    [Header("Full Energy Effect")]
    [SerializeField] private CanvasGroup fullEnergyEffect;

    [SerializeField, Min(0f)]
    private float fullEffectFadeSpeed = 5f;

    [SerializeField, Min(0f)]
    private float fullEffectPulseSpeed = 2f;

    [SerializeField, Min(0f)]
    private float fullEffectPulseScale = 0.04f;

    [Header("HUD Settings")]
    [SerializeField] private CanvasGroup orbHUDGroup;

    [SerializeField, Min(0f)]
    private float fillSmoothSpeed = 3f;

    private void Awake()
    {
        if (orbHUDGroup == null) orbHUDGroup = GetComponent<CanvasGroup>();
        if (orbHUDGroup == null) orbHUDGroup = gameObject.AddComponent<CanvasGroup>();
        // Stay hidden until Start resolves the player's orb ownership.
        orbHUDGroup.alpha = 0f;
        orbHUDGroup.interactable = false;
        orbHUDGroup.blocksRaycasts = false;
    }

    //플레이어 기력을 찾고 변경 이벤트를 구독
    private void Start()
    {
        CaptureDefaultStyle();
        EnsureEffectRoots();
        InitializeWaveFillMaterial();
        Canvas.willRenderCanvases += UpdateEnergyFill;

        playerEnergy = FindAnyObjectByType<PlayerEnergy>();

        if (playerEnergy == null)
        {
            Debug.LogError("OrbHUD: PlayerEnergy를 찾을 수 없습니다.", this);
            return;
        }

        if (fullEnergyEffect != null)
        {
            fullEffectStartScale = fullEnergyEffect.transform.localScale;
            fullEnergyEffect.alpha = 0f;
            fullEnergyEffect.interactable = false;
            fullEnergyEffect.blocksRaycasts = false;
        }

        playerEnergy.OnEnergyChanged += UpdateEnergyTarget;

        relicManager = playerEnergy.GetComponent<PlayerRelicManager>();
        playerAwakening = playerEnergy.GetComponent<PlayerAwakening>();
        playerSkill = playerEnergy.GetComponent<PlayerSkill>();
        SubscribeAwakeningEvents();
        if (relicManager != null)
        {
            relicManager.OnRelicsChanged += RefreshOrbDesign;
        }

        RefreshOrbDesign();

        UpdateEnergyTarget();

        //게임 시작 시에는 현재 기력 위치에서 바로 시작
        displayedEnergyRatio = targetEnergyRatio;
        UpdateEnergyFill();
    }

    private void OnDestroy()
    {
        Canvas.willRenderCanvases -= UpdateEnergyFill;
        UnsubscribeAwakeningEvents();
        DestroyRelicEffects();
        if (relicManager != null)
        {
            relicManager.OnRelicsChanged -= RefreshOrbDesign;
        }

        if (playerEnergy != null)
        {
            playerEnergy.OnEnergyChanged -= UpdateEnergyTarget;
        }

        if (orbImageFill != null
            && waveFillMaterial != null
            && orbImageFill.material == waveFillMaterial)
        {
            orbImageFill.material = originalOrbMaterial;
        }

        if (backdropImage != null && defaultStyle != null)
            ApplyBackdropColor(defaultStyle.backdropColor);

        if (waveFillMaterial != null)
        {
            Destroy(waveFillMaterial);
        }
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= UpdateEnergyFill;
        UnsubscribeAwakeningEvents();
        StopAwakeningEndEffect();
        StopPersistentEffect();
        if (fullChargeBurstInstance != null) fullChargeBurstInstance.SetActive(false);
        burstTimeRemaining = 0f;
        if (fullEnergyEffect != null) fullEnergyEffect.alpha = 0f;
        chargeBurstPending = false;
    }

    private void OnEnable()
    {
        SubscribeAwakeningEvents();
        if (waveFillMaterial != null)
        {
            Canvas.willRenderCanvases -= UpdateEnergyFill;
            Canvas.willRenderCanvases += UpdateEnergyFill;
        }
        // Reopening the HUD is not a new full-charge event.
        if (playerEnergy != null) wasChargeBurstReady = IsChargeBurstReady();
    }

    private void Update()
    {
        if (playerEnergy == null) return;

        //Inspector에서 Current Energy를 직접 바꾼 경우에도 HUD가 즉시 따라가게 함
        float currentEnergyRatio = GetEnergyDisplayRatio();

        bool currentEnergyFull = playerEnergy.HasOrb && playerEnergy.IsFull;

        if (!Mathf.Approximately(targetEnergyRatio, currentEnergyRatio)
            || isEnergyFull != currentEnergyFull)
        {
            UpdateEnergyTarget();
        }

        //기력 변화가 너무 딱딱하게 보이지 않도록 표시값을 부드럽게 변경
        displayedEnergyRatio = Mathf.MoveTowards(
            displayedEnergyRatio,
            targetEnergyRatio,
            fillSmoothSpeed * Time.unscaledDeltaTime
        );

        UpdateEnergyFill();
        UpdateRelicEffects();
        UpdateFullEnergyEffect();
    }
}
