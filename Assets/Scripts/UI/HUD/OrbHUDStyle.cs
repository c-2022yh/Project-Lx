using System;
using UnityEngine;

// Visual settings for an equipped orb relic. Missing sprites use the HUD defaults.
[Serializable]
public class OrbHUDStyle
{
    public Sprite emptySprite;
    public Sprite fillSprite;
    public Sprite fullEffectSprite;

    public Color effectColor = Color.white;

    [Min(0f)] public float fadeSpeed = 5f;
    [Min(0f)] public float pulseSpeed = 2f;
    [Min(0f)] public float pulseScale = 0.04f;
}
