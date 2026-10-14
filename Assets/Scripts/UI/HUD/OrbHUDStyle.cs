using System;
using UnityEngine;

// Visual settings for an equipped orb relic. Missing sprites use the HUD defaults.
[Serializable]
public class OrbHUDStyle
{
    public Sprite emptySprite;
    public Sprite fillSprite;
    public Sprite fullEffectSprite;

    [Header("Orb Decorations")]
    public Sprite frameSprite;
    public Sprite cloudSprite;
    [Tooltip("Color of the shared OrbBackDrop. Its sprite and material stay fixed; only this tint changes with the equipped relic.")]
    public Color backdropColor = Color.white;

    public Color effectColor = Color.white;

    [Min(0f)] public float fadeSpeed = 5f;
    [Min(0f)] public float pulseSpeed = 2f;
    [Min(0f)] public float pulseScale = 0.04f;
    [Range(0f, 1f)] public float pulseAlphaAmount;

    [Header("Effect Conditions")]
    public bool showFullEnergyEffect = true;
    public bool playFullChargeBurst = true;
    public OrbHUDEffectCondition persistentEffectCondition;

    [Tooltip("UI prefab shown while the selected condition holds. If empty, use the existing effect image and pulse.")]
    public GameObject persistentEffectPrefab;

    [Tooltip("Optional UI prefab played once on entering full energy, separately from the persistent effect.")]
    public GameObject fullChargeBurstPrefab;
    [Min(0.01f)] public float fullChargeBurstDuration = 1f;
    [Tooltip("Expansion of the eye-image burst used when no burst prefab is assigned.")]
    [Min(1f)] public float fullChargeBurstScale = 1.5f;
    [Range(0f, 1f)] public float fullChargeBurstAlpha = 0.8f;

    [Header("Gauge Display")]
    [Tooltip("0 uses the current maximum energy. Use 100 to display a reduced capacity such as Dark Moon's 50/100. Display only; does not change gameplay capacity.")]
    [Min(0f)] public float displayEnergyCapacity;
}

public enum OrbHUDEffectCondition
{
    None,
    Always,
    FullEnergy,
    Awakened,
    AwakeningEnergyDrain
}
