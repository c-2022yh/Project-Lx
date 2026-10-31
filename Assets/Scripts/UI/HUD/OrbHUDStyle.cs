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
    [InspectorName("Show Eye Effect")]
    public bool showFullEnergyEffect = true;
    [Tooltip("Independent of the additional persistent effect. Dark Moon: Always; Full Moon: FullEnergy.")]
    public OrbHUDEffectCondition eyeEffectCondition = OrbHUDEffectCondition.FullEnergy;
    public bool playFullChargeBurst = true;
    [Tooltip("Common eye burst threshold in actual energy units. Crescent gauges never play this burst.")]
    [Min(0.01f)] public float fullChargeEyeEnergy = 100f;
    public OrbHUDEffectCondition persistentEffectCondition;

    [Tooltip("Optional prefab shown while the selected condition holds. If empty, use Persistent Image Animation settings. Eye animation is independent.")]
    public GameObject persistentEffectPrefab;
    [Tooltip("Draw the persistent prefab in front of or behind the orb HUD.")]
    public OrbHUDEffectLayer persistentEffectLayer;

    [Header("Persistent Image Animation (No Prefab Required)")]
    public OrbHUDPersistentAnimation persistentAnimation;
    public Sprite persistentEffectSprite;
    [Tooltip("Ordered frames for flames/lightning. Null entries are ignored.")]
    public Sprite[] persistentEffectFrames = Array.Empty<Sprite>();
    public Color persistentEffectColor = Color.white;
    [Min(0.01f)] public float persistentSizeMultiplier = 1f;
    [Min(0.01f)] public float persistentPulsePeriod = 2.4f;
    public Vector2 persistentAlphaRange = new Vector2(0.38f, 0.8f);
    [Min(0.01f)] public float persistentFrameRate = 8f;
    [Range(0f, 1f)] public float persistentFrameCrossFade = 1f;
    [Tooltip("Quiet interval between flashes, in seconds. Does not include the flash duration.")]
    public Vector2 persistentFlashInterval = new Vector2(0.6f, 1.8f);
    [Tooltip("Intermittent only: hold one frame for each flash, cycling to the next shape on the next flash.")]
    public bool persistentSingleFramePerFlash;
    [Min(0.01f)] public float persistentFlashDuration = 0.3f;
    [Tooltip("Full-energy halo fades in/out; awakening/Q effects stop immediately when their condition ends. Zero means instant.")]
    [Min(0f)] public float persistentFadeSpeed = 5f;

    [Header("Full Charge Burst")]
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
    [Tooltip("Final scale relative to the base image size. Can be smaller than From Scale for a contracting pulse.")]
    [Min(0.01f)] public float awakeningEndFromScale = 1f;
    [Min(0.01f)] public float awakeningEndScale = 1.5f;
    [Tooltip("Fraction of the animation spent fading in. Reflux recovery: about 0.2.")]
    [Range(0.01f, 0.99f)] public float awakeningEndFadeInFraction = 0.2f;
    [Range(0f, 1f)] public float awakeningEndAlpha = 0.8f;

    [Header("Gauge Display")]
    [Tooltip("Wave mode: 0 uses the current maximum; a positive value sets a visual reference capacity. Crescent always uses the actual maximum. Display only.")]
    [Min(0f)] public float displayEnergyCapacity;
    public OrbHUDGaugeMode gaugeMode;
    [Tooltip("Optional material override. Assign OrbCrescentFillMaterial for Crescent so the shader is also included in builds.")]
    public Material gaugeMaterial;
    [Tooltip("Circle radius in the untrimmed image canvas, not the HUD rectangle size.")]
    [Range(0.01f, 0.5f)] public float crescentRadius = 0.38f;
    [Tooltip("Circle center in the original PNG canvas: (0.5, 0.5) is centered. Adjust only if the artwork itself is offset.")]
    public Vector2 crescentCenter = new Vector2(0.5f, 0.5f);
    [Tooltip("Maximum left crescent width as a fraction of the circle diameter. Energy progress controls growth to this shape.")]
    [Range(0.01f, 0.95f)] public float crescentWidth = 0.3f;
}

public enum OrbHUDEffectCondition
{
    None,
    Always,
    FullEnergy,
    Awakened,
    AwakeningEnergyDrain,
    AwakeningSkillReady
}

public enum OrbHUDPersistentAnimation
{
    Pulse,
    SpriteSequence,
    Intermittent
}

public enum OrbHUDGaugeMode
{
    Wave,
    Crescent
}

public enum OrbHUDEffectLayer
{
    Front,
    Back
}
