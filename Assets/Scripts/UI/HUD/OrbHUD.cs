using UnityEngine;
using UnityEngine.UI;

//플레이어의 현재 기력에 따라 보주 HUD를 갱신하는 스크립트
public class OrbHUD : MonoBehaviour
{
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");

    private PlayerEnergy playerEnergy;
    private PlayerRelicManager relicManager;
    private PlayerAwakening playerAwakening;

    [Header("Orb Design")]
    [SerializeField] private Image emptyImage;
    [SerializeField] private Image fullEffectImage;

    private OrbHUDStyle defaultStyle;
    private OrbHUDStyle activeStyle;
    private RelicData displayedRelic;
    private bool hasAppliedStyle;
    private GameObject persistentEffectInstance;
    private GameObject fullChargeBurstInstance;
    private float burstTimeRemaining;
    private bool wasEnergyFull;

    [Tooltip("Optional UI effect parent outside InnerMask. Defaults to this HUD's RectTransform.")]
    [SerializeField] private RectTransform effectRoot;

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

    private float targetEnergyRatio;
    private float displayedEnergyRatio;

    private bool isEnergyFull;

    private Vector3 fullEffectStartScale = Vector3.one;

    private Material originalOrbMaterial;
    private Material waveFillMaterial;

    //플레이어 기력을 찾고 변경 이벤트를 구독
    private void Start()
    {
        CaptureDefaultStyle();
        InitializeWaveFillMaterial();

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

        if (waveFillMaterial != null)
        {
            Destroy(waveFillMaterial);
        }
    }

    private void CaptureDefaultStyle()
    {
        // Also support existing HUD instances that have not assigned the new fields.
        if (emptyImage == null || fullEffectImage == null)
        {
            foreach (Image image in GetComponentsInChildren<Image>(true))
            {
                if (emptyImage == null && image.name == "EmptyImage") emptyImage = image;
                if (fullEffectImage == null && image.name == "FullEnergyEffect") fullEffectImage = image;
            }
        }

        defaultStyle = new OrbHUDStyle
        {
            emptySprite = emptyImage != null ? emptyImage.sprite : null,
            fillSprite = orbImageFill != null ? orbImageFill.sprite : null,
            fullEffectSprite = fullEffectImage != null ? fullEffectImage.sprite : null,
            effectColor = fullEffectImage != null ? fullEffectImage.color : Color.white,
            fadeSpeed = fullEffectFadeSpeed,
            pulseSpeed = fullEffectPulseSpeed,
            pulseScale = fullEffectPulseScale
        };
    }

    private void RefreshOrbDesign()
    {
        RelicData relic = relicManager != null ? relicManager.EquippedOrbRelic : null;
        // Ownership changes also emit this event; do not restart an unchanged equipped orb.
        if (hasAppliedStyle && displayedRelic == relic) return;

        DestroyRelicEffects();
        displayedRelic = relic;
        hasAppliedStyle = true;
        OrbHUDStyle style = relic != null && relic.OrbHUDStyle != null
            ? relic.OrbHUDStyle : defaultStyle;
        activeStyle = style;

        if (emptyImage != null)
            emptyImage.sprite = style.emptySprite != null ? style.emptySprite : defaultStyle.emptySprite;

        if (orbImageFill != null)
            orbImageFill.sprite = style.fillSprite != null ? style.fillSprite : defaultStyle.fillSprite;

        if (fullEffectImage != null)
        {
            fullEffectImage.sprite = style.fullEffectSprite != null
                ? style.fullEffectSprite : defaultStyle.fullEffectSprite;
            fullEffectImage.color = style.effectColor;
        }

        fullEffectFadeSpeed = Mathf.Max(0f, style.fadeSpeed);
        fullEffectPulseSpeed = Mathf.Max(0f, style.pulseSpeed);
        fullEffectPulseScale = Mathf.Max(0f, style.pulseScale);

        if (fullEnergyEffect != null)
        {
            fullEnergyEffect.alpha = 0f;
            fullEnergyEffect.transform.localScale = fullEffectStartScale;
        }

        persistentEffectInstance = CreateRelicEffect(style.persistentEffectPrefab);
        fullChargeBurstInstance = CreateRelicEffect(style.fullChargeBurstPrefab);
        UpdateEnergyTarget();
        // A reduced capacity must never retain a previously full visual gauge.
        displayedEnergyRatio = Mathf.Min(displayedEnergyRatio, targetEnergyRatio);
        // Equipping an already full orb restores its sustained effect, without a fake charge event.
        wasEnergyFull = isEnergyFull;
    }

    private GameObject CreateRelicEffect(GameObject prefab)
    {
        if (prefab == null) return null;
        if (prefab.GetComponent<RectTransform>() == null)
        {
            Debug.LogWarning("OrbHUD: HUD effect prefabs need a RectTransform (UI prefab).", this);
            return null;
        }

        GameObject instance = Instantiate(prefab, effectRoot != null ? effectRoot : transform, false);
        instance.SetActive(false);
        // Decorative effects must not intercept HUD clicks or drags.
        foreach (Graphic graphic in instance.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
        CanvasGroup group = instance.GetComponent<CanvasGroup>();
        if (group == null) group = instance.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        return instance;
    }

    private void DestroyRelicEffects()
    {
        if (persistentEffectInstance != null)
        {
            persistentEffectInstance.SetActive(false);
            Destroy(persistentEffectInstance);
        }
        if (fullChargeBurstInstance != null)
        {
            fullChargeBurstInstance.SetActive(false);
            Destroy(fullChargeBurstInstance);
        }
        persistentEffectInstance = null;
        fullChargeBurstInstance = null;
        burstTimeRemaining = 0f;
    }

    private void OnDisable()
    {
        if (persistentEffectInstance != null) persistentEffectInstance.SetActive(false);
        if (fullChargeBurstInstance != null) fullChargeBurstInstance.SetActive(false);
        burstTimeRemaining = 0f;
        if (fullEnergyEffect != null) fullEnergyEffect.alpha = 0f;
    }

    private void OnEnable()
    {
        // Reopening the HUD is not a new full-charge event.
        if (playerEnergy != null) wasEnergyFull = playerEnergy.HasOrb && playerEnergy.IsFull;
    }

    private float GetEnergyDisplayRatio()
    {
        if (playerEnergy == null || !playerEnergy.HasOrb) return 0f;
        float capacity = activeStyle != null && activeStyle.displayEnergyCapacity > 0f
            ? activeStyle.displayEnergyCapacity : playerEnergy.MaxEnergy;
        return capacity > 0f ? Mathf.Clamp01(playerEnergy.CurrentEnergy / capacity) : 0f;
    }

    private bool ShouldShowPersistentEffect()
    {
        if (activeStyle == null || playerEnergy == null || !playerEnergy.HasOrb) return false;
        switch (activeStyle.persistentEffectCondition)
        {
            case OrbHUDEffectCondition.Always:
                return true;
            case OrbHUDEffectCondition.FullEnergy:
                return isEnergyFull;
            case OrbHUDEffectCondition.Awakened:
                return playerAwakening != null && playerAwakening.IsAwakened;
            case OrbHUDEffectCondition.AwakeningEnergyDrain:
                return playerAwakening != null && playerAwakening.IsDrainingEnergy;
            default:
                return false;
        }
    }

    private void UpdateRelicEffects()
    {
        if (activeStyle == null) return;
        bool showPersistent = ShouldShowPersistentEffect();
        if (persistentEffectInstance != null && persistentEffectInstance.activeSelf != showPersistent)
            persistentEffectInstance.SetActive(showPersistent);

        if (activeStyle.playFullChargeBurst && isEnergyFull && !wasEnergyFull && fullChargeBurstInstance != null)
        {
            fullChargeBurstInstance.SetActive(false);
            fullChargeBurstInstance.SetActive(true);
            burstTimeRemaining = Mathf.Max(0.01f, activeStyle.fullChargeBurstDuration);
        }
        wasEnergyFull = isEnergyFull;

        if (fullChargeBurstInstance != null && fullChargeBurstInstance.activeSelf)
        {
            burstTimeRemaining -= Time.unscaledDeltaTime;
            if (!isEnergyFull || burstTimeRemaining <= 0f)
                fullChargeBurstInstance.SetActive(false);
        }
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

    //FullImage에 설정된 WaveFill Material을 복제해 이 HUD만의 기력값을 사용
    private void InitializeWaveFillMaterial()
    {
        if (orbImageFill == null)
        {
            Debug.LogError("OrbHUD: Orb Image Fill이 연결되지 않았습니다.", this);
            return;
        }

        originalOrbMaterial = orbImageFill.material;

        if (originalOrbMaterial == null
            || !originalOrbMaterial.HasProperty(FillAmountId))
        {
            Debug.LogError(
                "OrbHUD: Orb Image Fill에 UI/WaveFill Material을 설정해야 합니다.",
                this
            );
            return;
        }

        waveFillMaterial = new Material(originalOrbMaterial)
        {
            name = originalOrbMaterial.name + " (Runtime)"
        };

        orbImageFill.material = waveFillMaterial;
        waveFillMaterial.SetFloat(FillAmountId, 0f);

        //부모 Mask가 실제 렌더링용 Stencil Material을 다시 만들도록 갱신
        orbImageFill.SetMaterialDirty();
    }

    //PlayerEnergy의 현재 값으로 목표 표시 상태 갱신
    private void UpdateEnergyTarget()
    {
        if (playerEnergy == null) return;

        targetEnergyRatio = GetEnergyDisplayRatio();

        isEnergyFull = playerEnergy.HasOrb && playerEnergy.IsFull;

        if (orbHUDGroup != null)
        {
            //보주가 없어도 빈 HUD의 프레임과 구름은 항상 표시
            orbHUDGroup.alpha = 1f;
            orbHUDGroup.interactable = false;
            orbHUDGroup.blocksRaycasts = false;
        }
    }

    //현재 기력 비율을 WaveFill Shader에 전달해 100% 이미지를 아래부터 표시
    private void UpdateEnergyFill()
    {
        if (waveFillMaterial == null || orbImageFill == null) return;

        waveFillMaterial.SetFloat(FillAmountId, displayedEnergyRatio);

        //UI Mask 아래의 Image는 Unity가 Stencil Material을 별도로 만들어 사용한다.
        //원본 Material만 수정하면 화면에 반영되지 않으므로 실제 렌더링본도 갱신한다.
        Material renderMaterial = orbImageFill.materialForRendering;

        if (renderMaterial != null && renderMaterial.HasProperty(FillAmountId))
        {
            renderMaterial.SetFloat(FillAmountId, displayedEnergyRatio);
        }
    }

    //기력이 가득 차면 전용 이펙트를 표시하고 천천히 맥동시킴
    private void UpdateFullEnergyEffect()
    {
        if (fullEnergyEffect == null) return;

        bool showFullEffect = activeStyle != null
            && ((activeStyle.showFullEnergyEffect && isEnergyFull && displayedEnergyRatio >= 0.999f)
                || (persistentEffectInstance == null && ShouldShowPersistentEffect()));
        float targetAlpha = showFullEffect ? 1f : 0f;
        if (showFullEffect)
        {
            float shimmer = (Mathf.Sin(Time.unscaledTime * fullEffectPulseSpeed) + 1f) * 0.5f;
            targetAlpha -= Mathf.Clamp01(activeStyle.pulseAlphaAmount) * shimmer;
        }

        fullEnergyEffect.alpha = Mathf.MoveTowards(
            fullEnergyEffect.alpha,
            targetAlpha,
            fullEffectFadeSpeed * Time.unscaledDeltaTime
        );

        if (showFullEffect)
        {
            float pulse = 1f
                + Mathf.Sin(Time.unscaledTime * fullEffectPulseSpeed)
                * fullEffectPulseScale;

            fullEnergyEffect.transform.localScale = fullEffectStartScale * pulse;
        }
        else
        {
            fullEnergyEffect.transform.localScale = fullEffectStartScale;
        }
    }
}
