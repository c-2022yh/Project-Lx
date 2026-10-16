using UnityEngine;
using UnityEngine.UI;

// Energy display ratio, WaveFill material, and UI Mask rendering.
public partial class OrbHUD
{
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private float targetEnergyRatio;
    private float displayedEnergyRatio;
    private bool isEnergyFull;
    private Material originalOrbMaterial;
    private Material waveFillMaterial;

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

    private float GetEnergyDisplayRatio()
    {
        if (playerEnergy == null || !playerEnergy.HasOrb) return 0f;
        float capacity = activeStyle != null && activeStyle.displayEnergyCapacity > 0f
            ? activeStyle.displayEnergyCapacity : playerEnergy.MaxEnergy;
        return capacity > 0f ? Mathf.Clamp01(playerEnergy.CurrentEnergy / capacity) : 0f;
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
}
