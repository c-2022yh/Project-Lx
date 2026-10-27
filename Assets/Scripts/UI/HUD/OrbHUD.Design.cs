using UnityEngine;
using UnityEngine.UI;

// Default design, equipped relic sprites, and the shared backdrop tint.
public partial class OrbHUD
{
    private OrbHUDStyle defaultStyle;
    private OrbHUDStyle activeStyle;
    private RelicData displayedRelic;
    private bool hasAppliedStyle;
    private Material originalBackdropMaterial;
    private Sprite originalBackdropSprite;

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
        ConfigureGaugeMaterial(style);

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

        persistentEffectInstance = CreateRelicEffect(style.persistentEffectPrefab, style.persistentEffectLayer);
        if (style.persistentEffectPrefab == null)
            persistentEffectInstance = CreatePersistentImages();
        awakeningEndEffectInstance = CreateRelicEffect(style.awakeningEndEffectPrefab, style.awakeningEndEffectLayer);
        if (style.playAwakeningEndEffect && style.awakeningEndEffectPrefab == null
            && style.awakeningEndSprite != null)
            awakeningEndEffectInstance = CreateAwakeningEndImage();
        fullChargeBurstInstance = CreateRelicEffect(style.fullChargeBurstPrefab, style.fullChargeBurstLayer);
        if (fullChargeBurstInstance == null && style.fullChargeBurstPrefab == null
            && style.playFullChargeBurst)
            fullChargeBurstInstance = CreateFallbackChargeBurst();
        UpdateEnergyTarget();
        // A reduced capacity must never retain a previously full visual gauge.
        displayedEnergyRatio = Mathf.Min(displayedEnergyRatio, targetEnergyRatio);
        // Equipping an already full orb restores its sustained effect, without a fake charge event.
        wasChargeBurstReady = IsChargeBurstReady();
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
}
