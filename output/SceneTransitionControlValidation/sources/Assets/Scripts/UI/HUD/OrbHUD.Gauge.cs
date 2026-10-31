using UnityEngine;
using UnityEngine.UI;

// Energy display and per-HUD materials. The shared backdrop is never filled.
public partial class OrbHUD
{
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int SpriteUVRectId = Shader.PropertyToID("_SpriteUVRect");
    private float targetEnergyRatio;
    private float displayedEnergyRatio;
    private bool isEnergyFull;
    private Material originalOrbMaterial;
    private Material waveFillMaterial;
    private OrbHUDGaugeMode appliedGaugeMode;
    private Material appliedGaugeSource;

    private void ApplyEnergySprite(Image image, Sprite sprite)
    {
        if (image == null) return;
        image.overrideSprite = null;
        image.sprite = sprite;
        if (waveFillMaterial != null) image.material = waveFillMaterial;
        image.RecalculateMasking();
        image.SetAllDirty();
    }

    private float GetEnergyDisplayRatio()
    {
        if (playerEnergy == null || !playerEnergy.HasOrb) return 0f;
        // A full Crescent gauge completes a crescent, never a filled disc.
        float capacity = activeStyle != null && activeStyle.gaugeMode != OrbHUDGaugeMode.Crescent
            && activeStyle.displayEnergyCapacity > 0f ? activeStyle.displayEnergyCapacity : playerEnergy.MaxEnergy;
        return capacity > 0f ? Mathf.Clamp01(playerEnergy.CurrentEnergy / capacity) : 0f;
    }

    private bool IsChargeBurstReady()
    {
        return playerEnergy != null && activeStyle != null
            && OrbHUDAnimation.IsChargeReady(playerEnergy.HasOrb,
                activeStyle.gaugeMode == OrbHUDGaugeMode.Crescent,
                playerEnergy.CurrentEnergy, activeStyle.fullChargeEyeEnergy);
    }

    private void InitializeWaveFillMaterial()
    {
        if (orbImageFill == null)
        {
            Debug.LogError("OrbHUD: Assign FullImage to Orb Image Fill.", this);
            return;
        }
        originalOrbMaterial = orbImageFill.material;
        ConfigureGaugeMaterial(defaultStyle);
    }

    private void ConfigureGaugeMaterial(OrbHUDStyle style)
    {
        if (orbImageFill == null) return;
        appliedGaugeMode = style.gaugeMode;
        appliedGaugeSource = style.gaugeMaterial;
        string shaderName = style.gaugeMode == OrbHUDGaugeMode.Crescent ? "UI/OrbCrescentFill" : "UI/WaveFill";
        Material source = style.gaugeMaterial != null ? style.gaugeMaterial : originalOrbMaterial;
        bool validSource = source != null && source.shader != null
            && source.shader.name == shaderName && source.HasProperty(FillAmountId);
        Shader shader = validSource ? source.shader : Shader.Find(shaderName);
        if (shader == null || !shader.isSupported)
        {
            orbImageFill.enabled = false;
            Debug.LogError("OrbHUD: Missing/unsupported " + shaderName + ". Assign its material in Gauge Material.", this);
            return;
        }
        Material previous = waveFillMaterial;
        waveFillMaterial = validSource ? new Material(source) : new Material(shader);
        waveFillMaterial.name = shaderName + " (OrbHUD Runtime)";
        waveFillMaterial.SetFloat(FillAmountId, 0f);
        orbImageFill.enabled = true;
        orbImageFill.material = waveFillMaterial;
        orbImageFill.RecalculateMasking();
        orbImageFill.SetMaterialDirty();
        if (previous != null) Destroy(previous);
    }

    private void UpdateEnergyTarget()
    {
        if (playerEnergy == null) return;
        targetEnergyRatio = GetEnergyDisplayRatio();
        isEnergyFull = playerEnergy.HasOrb && playerEnergy.IsFull;
        if (orbHUDGroup != null)
        {
            orbHUDGroup.alpha = playerEnergy.HasOrb ? 1f : 0f;
            orbHUDGroup.interactable = false;
            orbHUDGroup.blocksRaycasts = false;
        }
    }

    private void UpdateEnergyFill()
    {
        // Mode/material edits on the equipped SO must not require re-equipping.
        if (activeStyle != null && (activeStyle.gaugeMode != appliedGaugeMode
            || activeStyle.gaugeMaterial != appliedGaugeSource))
            ConfigureGaugeMaterial(activeStyle);
        if (waveFillMaterial == null || orbImageFill == null) return;
        if (orbImageFill.material != waveFillMaterial)
        {
            orbImageFill.material = waveFillMaterial;
            orbImageFill.SetMaterialDirty();
        }
        WriteGaugeProperties(waveFillMaterial);
        Material rendered = orbImageFill.materialForRendering;
        if (rendered != null && rendered.HasProperty(FillAmountId)) WriteGaugeProperties(rendered);
    }

    private void WriteGaugeProperties(Material material)
    {
        material.SetFloat(FillAmountId, displayedEnergyRatio);
        if (!material.HasProperty(SpriteUVRectId)) return;
        Sprite sprite = orbImageFill.overrideSprite != null ? orbImageFill.overrideSprite : orbImageFill.sprite;
        Vector4 uv = sprite != null ? UnityEngine.Sprites.DataUtility.GetOuterUV(sprite) : new Vector4(0f, 0f, 1f, 1f);
        material.SetVector(SpriteUVRectId, uv);
        Vector4 canvasRect = new Vector4(0f, 0f, 1f, 1f);
        if (sprite != null)
        {
            Vector4 padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite);
            Vector2 size = sprite.rect.size;
            canvasRect = new Vector4(padding.x / Mathf.Max(1f, size.x), padding.y / Mathf.Max(1f, size.y),
                1f - padding.z / Mathf.Max(1f, size.x), 1f - padding.w / Mathf.Max(1f, size.y));
        }
        material.SetVector("_SpriteCanvasRect", canvasRect);
        Vector2 center = activeStyle != null ? activeStyle.crescentCenter : new Vector2(0.5f, 0.5f);
        material.SetVector("_CircleCenter", new Vector4(center.x, center.y, 0f, 0f));
        material.SetFloat("_CircleRadius", activeStyle != null ? activeStyle.crescentRadius : 0.38f);
        material.SetFloat("_CrescentWidth", activeStyle != null ? activeStyle.crescentWidth : 0.3f);
    }

    [ContextMenu("Log Gauge Rendering State")]
    private void LogGaugeRenderingState()
    {
        Material rendered = orbImageFill != null ? orbImageFill.materialForRendering : null;
        string shader = rendered != null && rendered.shader != null ? rendered.shader.name : "None";
        float fill = rendered != null && rendered.HasProperty(FillAmountId) ? rendered.GetFloat(FillAmountId) : -1f;
        Debug.Log($"OrbHUD Gauge: relic={displayedRelic?.RelicId}, mode={activeStyle?.gaugeMode}, "
            + $"energy={playerEnergy?.CurrentEnergy}/{playerEnergy?.MaxEnergy}, "
            + $"shader={shader}, fill={fill}, enabled={orbImageFill?.enabled}", this);
    }
}
