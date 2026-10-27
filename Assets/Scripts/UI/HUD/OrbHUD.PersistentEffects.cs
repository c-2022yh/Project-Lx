using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Shared sprite pulse, cross-faded flipbook, and intermittent flash playback.
public partial class OrbHUD
{
    private Image persistentImage;
    private Image persistentNextImage;
    private Sprite[] persistentFrames;
    private float persistentElapsed;
    private float persistentVisibility;
    private float nextFlashAt;
    private float flashStartedAt = -1f;
    private bool persistentConditionWasActive;

    private GameObject CreatePersistentImages()
    {
        var frames = new List<Sprite>();
        if (activeStyle.persistentEffectFrames != null)
            foreach (Sprite sprite in activeStyle.persistentEffectFrames)
                if (sprite != null) frames.Add(sprite);
        if (frames.Count == 0 && activeStyle.persistentEffectSprite != null)
            frames.Add(activeStyle.persistentEffectSprite);
        if (frames.Count == 0) return null;
        persistentFrames = frames.ToArray();

        RectTransform source = backdropImage != null ? backdropImage.rectTransform
            : orbImageFill != null ? orbImageFill.rectTransform : transform as RectTransform;
        if (source == null) return null;
        GameObject instance = new GameObject("PersistentOrbEffect", typeof(RectTransform), typeof(CanvasGroup));
        instance.layer = gameObject.layer;
        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.SetParent(source.parent, false);
        rect.anchorMin = source.anchorMin;
        rect.anchorMax = source.anchorMax;
        rect.pivot = source.pivot;
        rect.sizeDelta = source.sizeDelta;
        rect.anchoredPosition3D = source.anchoredPosition3D;
        rect.localRotation = source.localRotation;
        rect.localScale = source.localScale * Mathf.Max(0.01f, activeStyle.persistentSizeMultiplier);
        rect.SetParent(GetEffectRoot(activeStyle.persistentEffectLayer), true);
        CanvasGroup group = instance.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        persistentImage = CreatePersistentImage(rect, "CurrentFrame");
        persistentNextImage = CreatePersistentImage(rect, "NextFrame");
        instance.SetActive(false);
        return instance;
    }

    private Image CreatePersistentImage(RectTransform parent, string imageName)
    {
        var instance = new GameObject(imageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        instance.layer = gameObject.layer;
        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = instance.GetComponent<Image>();
        image.sprite = persistentFrames[0];
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.clear;
        return image;
    }

    private void ResetPersistentImages()
    {
        persistentImage = null;
        persistentNextImage = null;
        persistentFrames = null;
    }

    private void StopPersistentEffect()
    {
        if (persistentEffectInstance != null) persistentEffectInstance.SetActive(false);
        persistentConditionWasActive = false;
        persistentVisibility = 0f;
        persistentElapsed = 0f;
        nextFlashAt = 0f;
        flashStartedAt = -1f;
    }

    private float FlashInterval()
    {
        float low = Mathf.Max(0f, Mathf.Min(activeStyle.persistentFlashInterval.x, activeStyle.persistentFlashInterval.y));
        float high = Mathf.Max(low, Mathf.Max(activeStyle.persistentFlashInterval.x, activeStyle.persistentFlashInterval.y));
        return Random.Range(low, high);
    }

    private void UpdatePersistentEffect()
    {
        if (persistentEffectInstance == null) return;
        bool condition = ShouldShowPersistentEffect();
        // Q consumption / awakening end must remove lightning or flames immediately.
        if (!condition && activeStyle.persistentEffectCondition != OrbHUDEffectCondition.FullEnergy)
        {
            StopPersistentEffect();
            return;
        }
        if (condition && !persistentConditionWasActive)
        {
            persistentElapsed = 0f;
            flashStartedAt = -1f;
            nextFlashAt = FlashInterval();
            persistentEffectInstance.SetActive(true);
        }
        persistentConditionWasActive = condition;
        float target = condition ? 1f : 0f;
        persistentVisibility = activeStyle.persistentFadeSpeed <= 0f ? target
            : Mathf.MoveTowards(persistentVisibility, target, activeStyle.persistentFadeSpeed * Time.unscaledDeltaTime);
        if (!condition && persistentVisibility <= 0f)
        {
            StopPersistentEffect();
            return;
        }
        CanvasGroup group = persistentEffectInstance.GetComponent<CanvasGroup>();
        if (group != null) group.alpha = persistentVisibility;
        persistentElapsed += Time.unscaledDeltaTime;
        // Custom prefabs own their animation; only generated Images use this playback.
        if (persistentImage == null) return;

        float time = persistentElapsed;
        float alpha = 1f;
        if (activeStyle.persistentAnimation == OrbHUDPersistentAnimation.Pulse)
        {
            alpha = OrbHUDAnimation.PulseAlpha(time, activeStyle.persistentPulsePeriod,
                activeStyle.persistentAlphaRange.x, activeStyle.persistentAlphaRange.y);
        }
        else if (activeStyle.persistentAnimation == OrbHUDPersistentAnimation.Intermittent)
        {
            float duration = Mathf.Max(0.01f, activeStyle.persistentFlashDuration);
            if (time >= nextFlashAt)
            {
                flashStartedAt = time;
                nextFlashAt = time + duration + FlashInterval();
            }
            time = flashStartedAt < 0f ? -1f : time - flashStartedAt;
            alpha = time < 0f || time >= duration ? 0f : OrbHUDAnimation.BurstAlpha(time / duration, 0.2f);
        }

        bool sequence = activeStyle.persistentAnimation != OrbHUDPersistentAnimation.Pulse;
        int index = sequence ? OrbHUDAnimation.FrameIndex(time, activeStyle.persistentFrameRate, persistentFrames.Length) : 0;
        float blend = sequence && persistentFrames.Length > 1
            ? OrbHUDAnimation.FrameBlend(time, activeStyle.persistentFrameRate, activeStyle.persistentFrameCrossFade) : 0f;
        persistentImage.sprite = persistentFrames[index];
        persistentNextImage.sprite = persistentFrames[(index + 1) % persistentFrames.Length];
        Color color = activeStyle.persistentEffectColor;
        color.a *= alpha * (1f - blend);
        persistentImage.color = color;
        color = activeStyle.persistentEffectColor;
        color.a *= alpha * blend;
        persistentNextImage.color = color;
    }
}
