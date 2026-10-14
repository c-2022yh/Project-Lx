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
    [SerializeField] private Image frameImage;
    [SerializeField] private Image cloudImage;
    [SerializeField] private Image backdropImage;

    private OrbHUDStyle defaultStyle;
    private OrbHUDStyle activeStyle;
    private RelicData displayedRelic;
    private bool hasAppliedStyle;
    private GameObject persistentEffectInstance;
    private GameObject fullChargeBurstInstance;
    private float burstTimeRemaining;
    private bool wasEnergyFull;
    private bool chargeBurstPending;
    private Image fallbackBurstImage;

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
    private Material originalBackdropMaterial;
    private Sprite originalBackdropSprite;
    private Material waveFillMaterial;

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

    private void CaptureDefaultStyle()
    {
        // Also support existing HUD instances that have not assigned the new fields.
        if (orbImageFill == null || emptyImage == null || fullEffectImage == null
            || frameImage == null || cloudImage == null || backdropImage == null)
        {
            foreach (Image image in GetComponentsInChildren<Image>(true))
            {
                if (orbImageFill == null && image.name == "FullImage") orbImageFill = image;
                if (emptyImage == null && image.name == "EmptyImage") emptyImage = image;
                if (fullEffectImage == null && image.name == "FullEnergyEffect") fullEffectImage = image;
                if (frameImage == null && image.name == "OrbFrame") frameImage = image;
                if (cloudImage == null && image.name == "OrbCloud") cloudImage = image;
                if (backdropImage == null && image.name == "OrbBackDrop") backdropImage = image;
            }
        }

        originalBackdropMaterial = backdropImage != null ? backdropImage.material : null;
        originalBackdropSprite = backdropImage != null ? backdropImage.sprite : null;

        defaultStyle = new OrbHUDStyle
        {
            emptySprite = emptyImage != null ? emptyImage.sprite : null,
            fillSprite = orbImageFill != null ? orbImageFill.sprite : null,
            fullEffectSprite = fullEffectImage != null ? fullEffectImage.sprite : null,
            frameSprite = frameImage != null ? frameImage.sprite : null,
            cloudSprite = cloudImage != null ? cloudImage.sprite : null,
            backdropColor = backdropImage != null ? backdropImage.color : Color.white,
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

        ApplyEnergySprite(orbImageFill,
            style.fillSprite != null ? style.fillSprite : defaultStyle.fillSprite);

        if (fullEffectImage != null)
        {
            fullEffectImage.sprite = style.fullEffectSprite != null
                ? style.fullEffectSprite : defaultStyle.fullEffectSprite;
            fullEffectImage.color = style.effectColor;
        }

        if (frameImage != null)
            frameImage.sprite = style.frameSprite != null ? style.frameSprite : defaultStyle.frameSprite;

        if (cloudImage != null)
            cloudImage.sprite = style.cloudSprite != null ? style.cloudSprite : defaultStyle.cloudSprite;

        ApplyBackdropColor(style.backdropColor);
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
        if (fullChargeBurstInstance == null && style.fullChargeBurstPrefab == null
            && style.playFullChargeBurst)
            fullChargeBurstInstance = CreateFallbackChargeBurst();
        UpdateEnergyTarget();
        // A reduced capacity must never retain a previously full visual gauge.
        displayedEnergyRatio = Mathf.Min(displayedEnergyRatio, targetEnergyRatio);
        // Equipping an already full orb restores its sustained effect, without a fake charge event.
        wasEnergyFull = isEnergyFull;
        UpdateEnergyFill();
    }

    private void ApplyBackdropColor(Color color)
    {
        if (backdropImage == null) return;
        // OrbBackDrop is the constant base, never an energy-fill layer.
        backdropImage.overrideSprite = null;
        backdropImage.sprite = originalBackdropSprite;
        backdropImage.material = originalBackdropMaterial;
        backdropImage.color = color;
        backdropImage.RecalculateMasking();
        backdropImage.SetAllDirty();
    }
    private void ApplyEnergySprite(Image image, Sprite sprite)
    {
        if (image == null) return;
        // An override sprite can otherwise hide changes to the source sprite.
        image.overrideSprite = null;
        image.sprite = sprite;
        if (waveFillMaterial != null)
            image.material = waveFillMaterial;
        image.RecalculateMasking();
        image.SetAllDirty();
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

    //전용 프리팹이 없으면 현재 눈 이미지와 같은 위치에 완충 연출을 생성
    private GameObject CreateFallbackChargeBurst()
    {
        if (fullEffectImage == null) return null;

        GameObject instance = new GameObject("FullChargeBurst", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        RectTransform source = fullEffectImage.rectTransform;
        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.SetParent(source.parent, false);
        rect.anchorMin = source.anchorMin;
        rect.anchorMax = source.anchorMax;
        rect.pivot = source.pivot;
        rect.sizeDelta = source.sizeDelta;
        rect.anchoredPosition3D = source.anchoredPosition3D;
        rect.localRotation = source.localRotation;
        rect.localScale = fullEffectStartScale;
        rect.SetSiblingIndex(source.GetSiblingIndex() + 1);
        instance.layer = fullEffectImage.gameObject.layer;

        fallbackBurstImage = instance.GetComponent<Image>();
        fallbackBurstImage.sprite = fullEffectImage.sprite;
        fallbackBurstImage.material = fullEffectImage.material;
        fallbackBurstImage.preserveAspect = fullEffectImage.preserveAspect;
        fallbackBurstImage.raycastTarget = false;
        instance.SetActive(false);
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
        fallbackBurstImage = null;
        chargeBurstPending = false;
        burstTimeRemaining = 0f;
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= UpdateEnergyFill;
        if (persistentEffectInstance != null) persistentEffectInstance.SetActive(false);
        if (fullChargeBurstInstance != null) fullChargeBurstInstance.SetActive(false);
        burstTimeRemaining = 0f;
        if (fullEnergyEffect != null) fullEnergyEffect.alpha = 0f;
        chargeBurstPending = false;
    }

    private void OnEnable()
    {
        if (waveFillMaterial != null)
        {
            Canvas.willRenderCanvases -= UpdateEnergyFill;
            Canvas.willRenderCanvases += UpdateEnergyFill;
        }
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

        if (activeStyle.playFullChargeBurst && isEnergyFull && !wasEnergyFull)
            chargeBurstPending = true;
        if (!isEnergyFull || !activeStyle.playFullChargeBurst)
            chargeBurstPending = false;

        //표시 게이지가 목표까지 찬 뒤 재생. 암월의 50/100 표시도 같은 기준 사용.
        if (chargeBurstPending && displayedEnergyRatio >= targetEnergyRatio - 0.001f
            && fullChargeBurstInstance != null)
        {
            chargeBurstPending = false;
            fullChargeBurstInstance.SetActive(false);
            fullChargeBurstInstance.SetActive(true);
            burstTimeRemaining = Mathf.Max(0.01f, activeStyle.fullChargeBurstDuration);
        }
        wasEnergyFull = isEnergyFull;

        if (fullChargeBurstInstance != null && fullChargeBurstInstance.activeSelf)
        {
            burstTimeRemaining -= Time.unscaledDeltaTime;
            UpdateFallbackChargeBurst();
            if (!isEnergyFull || burstTimeRemaining <= 0f)
                fullChargeBurstInstance.SetActive(false);
        }
    }

    //완충 눈 이미지를 한 번 확대하고 부드럽게 사라지게 함
    private void UpdateFallbackChargeBurst()
    {
        if (fallbackBurstImage == null) return;
        float duration = Mathf.Max(0.01f, activeStyle.fullChargeBurstDuration);
        float progress = Mathf.Clamp01(1f - burstTimeRemaining / duration);
        float expansion = Mathf.Lerp(1f, Mathf.Max(1f, activeStyle.fullChargeBurstScale),
            1f - Mathf.Pow(1f - progress, 2f));
        fallbackBurstImage.rectTransform.localScale = fullEffectStartScale * expansion;
        Color color = activeStyle.effectColor;
        color.a *= Mathf.Clamp01(activeStyle.fullChargeBurstAlpha) * Mathf.Sin(progress * Mathf.PI);
        fallbackBurstImage.color = color;
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
            Debug.LogError("OrbHUD: Assign the FullImage Image to Orb Image Fill.", this);
            return;
        }

        originalOrbMaterial = orbImageFill.material;
        bool validSource = originalOrbMaterial != null
            && originalOrbMaterial.shader != null
            && originalOrbMaterial.shader.name == "UI/WaveFill"
            && originalOrbMaterial.HasProperty(FillAmountId);

        if (validSource)
        {
            waveFillMaterial = new Material(originalOrbMaterial);
        }
        else
        {
            Shader shader = Shader.Find("UI/WaveFill");
            if (shader == null || !shader.isSupported)
            {
                // Do not display a misleading full image if the shader cannot be loaded.
                orbImageFill.enabled = false;
                Debug.LogError("OrbHUD: UI/WaveFill shader is missing or unsupported. Charge image hidden.", this);
                return;
            }
            waveFillMaterial = new Material(shader);
            Debug.LogWarning("OrbHUD: FullImage did not have UI/WaveFill. Applied it at runtime.", this);
        }

        waveFillMaterial.name = "OrbHUD WaveFill (Runtime)";
        waveFillMaterial.SetFloat(FillAmountId, 0f);
        orbImageFill.material = waveFillMaterial;
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
            // Show the entire orb HUD only after acquiring the orb.
            orbHUDGroup.alpha = playerEnergy.HasOrb ? 1f : 0f;
            orbHUDGroup.interactable = false;
            orbHUDGroup.blocksRaycasts = false;
        }
    }

    //현재 기력 비율을 WaveFill Shader에 전달해 100% 이미지를 아래부터 표시
    private void UpdateEnergyFill()
    {
        if (waveFillMaterial == null || orbImageFill == null) return;

        // Reapply if an animation or another UI script replaced the runtime material.
        if (orbImageFill.material != waveFillMaterial)
        {
            orbImageFill.material = waveFillMaterial;
            orbImageFill.SetMaterialDirty();
        }
        waveFillMaterial.SetFloat(FillAmountId, displayedEnergyRatio);

        //UI Mask 아래의 Image는 Unity가 Stencil Material을 별도로 만들어 사용한다.
        //원본 Material만 수정하면 화면에 반영되지 않으므로 실제 렌더링본도 갱신한다.
        UpdateRenderedFill(orbImageFill);
    }

    private void UpdateRenderedFill(Image image)
    {
        Material renderMaterial = image.materialForRendering;
        if (renderMaterial != null && renderMaterial.HasProperty(FillAmountId))
            renderMaterial.SetFloat(FillAmountId, displayedEnergyRatio);
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
