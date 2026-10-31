using UnityEngine;
using UnityEngine.UI;

// Effect conditions, event subscriptions, layers, and effect lifetime.
public partial class OrbHUD
{
    private GameObject persistentEffectInstance;
    private GameObject fullChargeBurstInstance;
    private GameObject awakeningEndEffectInstance;
    private float awakeningEndTimeRemaining;
    private float burstTimeRemaining;
    private bool wasChargeBurstReady;
    private bool chargeBurstPending;

    private void EnsureEffectRoots()
    {
        if (backEffectRoot == null || backEffectRoot == transform)
            backEffectRoot = FindOrCreateEffectRoot("EffectsBack");
        if (frontEffectRoot == null || frontEffectRoot == transform || frontEffectRoot == backEffectRoot)
            frontEffectRoot = FindOrCreateEffectRoot("EffectsFront");

        if (backEffectRoot.parent == transform) backEffectRoot.SetAsFirstSibling();
        if (frontEffectRoot.parent == transform) frontEffectRoot.SetAsLastSibling();
    }

    private RectTransform FindOrCreateEffectRoot(string rootName)
    {
        RectTransform root = transform.Find(rootName) as RectTransform;
        if (root != null) return root;

        GameObject layer = new GameObject(rootName, typeof(RectTransform));
        layer.layer = gameObject.layer;
        root = layer.GetComponent<RectTransform>();
        root.SetParent(transform, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        return root;
    }

    private RectTransform GetEffectRoot(OrbHUDEffectLayer layer)
    {
        EnsureEffectRoots();
        return layer == OrbHUDEffectLayer.Back ? backEffectRoot : frontEffectRoot;
    }

    private GameObject CreateRelicEffect(GameObject prefab, OrbHUDEffectLayer layer)
    {
        if (prefab == null) return null;
        if (prefab.GetComponent<RectTransform>() == null)
        {
            Debug.LogWarning("OrbHUD: HUD effect prefabs need a RectTransform (UI prefab).", this);
            return null;
        }

        GameObject instance = Instantiate(prefab, GetEffectRoot(layer), false);
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
        StopPersistentEffect();
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
        StopAwakeningEndEffect();
        if (awakeningEndEffectInstance != null)
            Destroy(awakeningEndEffectInstance);
        awakeningEndEffectInstance = null;
        awakeningEndImage = null;
        persistentEffectInstance = null;
        ResetPersistentImages();
        fullChargeBurstInstance = null;
        fallbackBurstImage = null;
        chargeBurstPending = false;
        burstTimeRemaining = 0f;
    }

    private void SubscribeAwakeningEvents()
    {
        if (playerAwakening == null) return;
        // Start and OnEnable may both connect; keep exactly one subscription.
        UnsubscribeAwakeningEvents();
        playerAwakening.OnAwakeningEnded += HandleAwakeningEnded;
        playerAwakening.OnAwakeningStarted += StopAwakeningEndEffect;
    }

    private void UnsubscribeAwakeningEvents()
    {
        if (playerAwakening == null) return;
        playerAwakening.OnAwakeningEnded -= HandleAwakeningEnded;
        playerAwakening.OnAwakeningStarted -= StopAwakeningEndEffect;
    }

    private void HandleAwakeningEnded()
    {
        if (!isActiveAndEnabled || activeStyle == null || !activeStyle.playAwakeningEndEffect
            || awakeningEndEffectInstance == null || playerEnergy == null || !playerEnergy.HasOrb)
            return;
        // Ignore a stale style during an equipment change that ends awakening.
        if (relicManager == null || displayedRelic != relicManager.EquippedOrbRelic) return;

        awakeningEndTimeRemaining = Mathf.Max(0.01f, activeStyle.awakeningEndDuration);
        UpdateAwakeningEndImage();
        awakeningEndEffectInstance.SetActive(false);
        awakeningEndEffectInstance.SetActive(true);
    }

    private void StopAwakeningEndEffect()
    {
        if (awakeningEndEffectInstance != null) awakeningEndEffectInstance.SetActive(false);
        awakeningEndTimeRemaining = 0f;
    }

    private void UpdateAwakeningEndEffect()
    {
        if (awakeningEndEffectInstance == null || !awakeningEndEffectInstance.activeSelf) return;
        // This timer is independent of energy: awakening can end at zero energy,
        // and Reflux can restore energy in another listener of the same event.
        awakeningEndTimeRemaining -= Time.unscaledDeltaTime;
        UpdateAwakeningEndImage();
        if (!activeStyle.playAwakeningEndEffect || !playerEnergy.HasOrb || awakeningEndTimeRemaining <= 0f)
            StopAwakeningEndEffect();
    }

    private bool ShouldShowPersistentEffect()
        => activeStyle != null && EvaluateEffectCondition(activeStyle.persistentEffectCondition);

    private bool EvaluateEffectCondition(OrbHUDEffectCondition condition)
    {
        if (activeStyle == null || playerEnergy == null || !playerEnergy.HasOrb) return false;
        switch (condition)
        {
            case OrbHUDEffectCondition.Always:
                return true;
            case OrbHUDEffectCondition.FullEnergy:
                return isEnergyFull;
            case OrbHUDEffectCondition.Awakened:
                return playerAwakening != null && playerAwakening.IsAwakened;
            case OrbHUDEffectCondition.AwakeningEnergyDrain:
                return playerAwakening != null && playerAwakening.IsDrainingEnergy;
            case OrbHUDEffectCondition.AwakeningSkillReady:
                return playerAwakening != null && playerAwakening.IsAwakened
                    && playerSkill != null && playerSkill.HasAwakeningSkillCharge;
            default:
                return false;
        }
    }

    private void UpdateRelicEffects()
    {
        if (activeStyle == null) return;
        UpdateAwakeningEndEffect();
        UpdatePersistentEffect();

        bool chargeReady = IsChargeBurstReady();
        if (activeStyle.playFullChargeBurst && chargeReady && !wasChargeBurstReady)
            chargeBurstPending = true;
        if (!chargeReady || !activeStyle.playFullChargeBurst)
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
        wasChargeBurstReady = chargeReady;

        if (fullChargeBurstInstance != null && fullChargeBurstInstance.activeSelf)
        {
            burstTimeRemaining -= Time.unscaledDeltaTime;
            UpdateFallbackChargeBurst();
            if (!chargeReady || burstTimeRemaining <= 0f)
                fullChargeBurstInstance.SetActive(false);
        }
    }
}
