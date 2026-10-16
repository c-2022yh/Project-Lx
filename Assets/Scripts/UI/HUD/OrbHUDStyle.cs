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
    [Tooltip("Draw the persistent prefab in front of or behind the orb HUD.")]
    public OrbHUDEffectLayer persistentEffectLayer;

    [Tooltip("Optional UI prefab played once on entering full energy, separately from the persistent effect.")]
    public GameObject fullChargeBurstPrefab;
    public OrbHUDEffectLayer fullChargeBurstLayer;
    [Min(0.01f)] public float fullChargeBurstDuration = 1f;
    [Tooltip("Expansion of the eye-image burst used when no burst prefab is assigned.")]
    [Min(1f)] public float fullChargeBurstScale = 1.5f;
    [Range(0f, 1f)] public float fullChargeBurstAlpha = 0.8f;

    [Header("Awakening End Effect")]
    public bool playAwakeningEndEffect;
    [Tooltip("Optional UI prefab played once when awakening ends. Takes priority over the sprite animation below.")]
    public GameObject awakeningEndEffectPrefab;
    public OrbHUDEffectLayer awakeningEndEffectLayer;
    [Min(0.01f)] public float awakeningEndDuration = 1f;
    [Tooltip("When no prefab is assigned, animate this sprite with expansion and a fade. No Animator required.")]
    public Sprite awakeningEndSprite;
    public Color awakeningEndColor = Color.white;
    [Tooltip("Initial image size relative to the shared orb backdrop. HUD scale is inherited automatically.")]
    [Min(0.01f)] public float awakeningEndSizeMultiplier = 1f;
    [Tooltip("Final expansion relative to the initial image size.")]
    [Min(1f)] public float awakeningEndScale = 1.5f;
    [Range(0f, 1f)] public float awakeningEndAlpha = 0.8f;

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

public enum OrbHUDEffectLayer
{
    Front,
    Back
}
