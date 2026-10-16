using UnityEngine;
using UnityEngine.UI;

// Eye shimmer and image-based charge/awakening-end animations.
public partial class OrbHUD
{
    private Image awakeningEndImage;
    private Vector3 awakeningEndStartScale;
    private Image fallbackBurstImage;
    private Vector3 fullEffectStartScale = Vector3.one;

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
        // Preserve the source eye alignment while placing the burst on the selected layer.
        rect.SetParent(GetEffectRoot(activeStyle.fullChargeBurstLayer), true);
        instance.layer = fullEffectImage.gameObject.layer;

        fallbackBurstImage = instance.GetComponent<Image>();
        fallbackBurstImage.sprite = fullEffectImage.sprite;
        fallbackBurstImage.material = fullEffectImage.material;
        fallbackBurstImage.preserveAspect = fullEffectImage.preserveAspect;
        fallbackBurstImage.raycastTarget = false;
        instance.SetActive(false);
        return instance;
    }

    private GameObject CreateAwakeningEndImage()
    {
        RectTransform source = backdropImage != null ? backdropImage.rectTransform
            : orbImageFill != null ? orbImageFill.rectTransform : transform as RectTransform;
        if (source == null) return null;

        GameObject instance = new GameObject("AwakeningEndImage", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        instance.layer = gameObject.layer;
        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.SetParent(source.parent, false);
        rect.anchorMin = source.anchorMin;
        rect.anchorMax = source.anchorMax;
        rect.pivot = source.pivot;
        rect.sizeDelta = source.sizeDelta;
        rect.anchoredPosition3D = source.anchoredPosition3D;
        rect.localRotation = source.localRotation;
        rect.localScale = source.localScale * Mathf.Max(0.01f, activeStyle.awakeningEndSizeMultiplier);
        rect.SetParent(GetEffectRoot(activeStyle.awakeningEndEffectLayer), true);
        awakeningEndStartScale = rect.localScale;

        awakeningEndImage = instance.GetComponent<Image>();
        awakeningEndImage.sprite = activeStyle.awakeningEndSprite;
        awakeningEndImage.preserveAspect = true;
        awakeningEndImage.raycastTarget = false;
        Color color = activeStyle.awakeningEndColor;
        color.a = 0f;
        awakeningEndImage.color = color;
        // Use the default UI material, independently of the energy-fill shader.
        instance.SetActive(false);
        return instance;
    }

    private void UpdateAwakeningEndImage()
    {
        if (awakeningEndImage == null) return;
        AnimateBurstImage(awakeningEndImage, awakeningEndStartScale, activeStyle.awakeningEndColor,
            awakeningEndTimeRemaining, activeStyle.awakeningEndDuration,
            activeStyle.awakeningEndScale, activeStyle.awakeningEndAlpha);
    }

    //완충 눈 이미지를 한 번 확대하고 부드럽게 사라지게 함
    private void UpdateFallbackChargeBurst()
    {
        if (fallbackBurstImage == null) return;
        AnimateBurstImage(fallbackBurstImage, fullEffectStartScale, activeStyle.effectColor,
            burstTimeRemaining, activeStyle.fullChargeBurstDuration,
            activeStyle.fullChargeBurstScale, activeStyle.fullChargeBurstAlpha);
    }

    private static void AnimateBurstImage(Image image, Vector3 startScale, Color color,
        float timeRemaining, float duration, float endScale, float alpha)
    {
        float progress = Mathf.Clamp01(1f - timeRemaining / Mathf.Max(0.01f, duration));
        float expansion = Mathf.Lerp(1f, Mathf.Max(1f, endScale),
            1f - Mathf.Pow(1f - progress, 2f));
        image.rectTransform.localScale = startScale * expansion;
        color.a *= Mathf.Clamp01(alpha) * Mathf.Sin(progress * Mathf.PI);
        image.color = color;
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
