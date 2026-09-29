using UnityEngine;
using UnityEngine.UI;

//플레이어의 현재 기력에 따라 보주 HUD를 갱신하는 스크립트
public class OrbHUD : MonoBehaviour
{
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");

    private PlayerEnergy playerEnergy;

    [Header("Energy Fill")]
    [SerializeField] private Image orbImageFill;

    [Header("Full Energy Effect")]
    [SerializeField] private CanvasGroup fullEnergyEffect;

    [SerializeField, Min(0f)]
    private float fullEffectFadeSpeed = 5f;

    [SerializeField, Min(0f)]
    private float fullEffectPulseSpeed = 2f;

    [SerializeField, Min(0f)]
    private float fullEffectPulseScale = 0.04f;

    [Header("HUD Settings")]
    [SerializeField] private CanvasGroup orbHUDGroup;

    [SerializeField, Min(0f)]
    private float fillSmoothSpeed = 3f;

    private float targetEnergyRatio;
    private float displayedEnergyRatio;

    private bool isEnergyFull;

    private Vector3 fullEffectStartScale = Vector3.one;

    private Material originalOrbMaterial;
    private Material waveFillMaterial;

    //플레이어 기력을 찾고 변경 이벤트를 구독
    private void Start()
    {
        InitializeWaveFillMaterial();

        playerEnergy = FindAnyObjectByType<PlayerEnergy>();

        if (playerEnergy == null)
        {
            Debug.LogError("OrbHUD: PlayerEnergy를 찾을 수 없습니다.", this);
            return;
        }

        if (fullEnergyEffect != null)
        {
            fullEffectStartScale = fullEnergyEffect.transform.localScale;
            fullEnergyEffect.alpha = 0f;
            fullEnergyEffect.interactable = false;
            fullEnergyEffect.blocksRaycasts = false;
        }

        playerEnergy.OnEnergyChanged += UpdateEnergyTarget;

        UpdateEnergyTarget();

        //게임 시작 시에는 현재 기력 위치에서 바로 시작
        displayedEnergyRatio = targetEnergyRatio;
        UpdateEnergyFill();
    }

    private void OnDestroy()
    {
        if (playerEnergy != null)
        {
            playerEnergy.OnEnergyChanged -= UpdateEnergyTarget;
        }

        if (orbImageFill != null
            && waveFillMaterial != null
            && orbImageFill.material == waveFillMaterial)
        {
            orbImageFill.material = originalOrbMaterial;
        }

        if (waveFillMaterial != null)
        {
            Destroy(waveFillMaterial);
        }
    }

    private void Update()
    {
        if (playerEnergy == null) return;

        //Inspector에서 Current Energy를 직접 바꾼 경우에도 HUD가 즉시 따라가게 함
        float currentEnergyRatio = playerEnergy.HasOrb
            ? Mathf.Clamp01(playerEnergy.EnergyRatio)
            : 0f;

        bool currentEnergyFull = playerEnergy.HasOrb && playerEnergy.IsFull;

        if (!Mathf.Approximately(targetEnergyRatio, currentEnergyRatio)
            || isEnergyFull != currentEnergyFull)
        {
            UpdateEnergyTarget();
        }

        //기력 변화가 너무 딱딱하게 보이지 않도록 표시값을 부드럽게 변경
        displayedEnergyRatio = Mathf.MoveTowards(
            displayedEnergyRatio,
            targetEnergyRatio,
            fillSmoothSpeed * Time.unscaledDeltaTime
        );

        UpdateEnergyFill();
        UpdateFullEnergyEffect();
    }

    //FullImage에 설정된 WaveFill Material을 복제해 이 HUD만의 기력값을 사용
    private void InitializeWaveFillMaterial()
    {
        if (orbImageFill == null)
        {
            Debug.LogError("OrbHUD: Orb Image Fill이 연결되지 않았습니다.", this);
            return;
        }

        originalOrbMaterial = orbImageFill.material;

        if (originalOrbMaterial == null
            || !originalOrbMaterial.HasProperty(FillAmountId))
        {
            Debug.LogError(
                "OrbHUD: Orb Image Fill에 UI/WaveFill Material을 설정해야 합니다.",
                this
            );
            return;
        }

        waveFillMaterial = new Material(originalOrbMaterial)
        {
            name = originalOrbMaterial.name + " (Runtime)"
        };

        orbImageFill.material = waveFillMaterial;
        waveFillMaterial.SetFloat(FillAmountId, 0f);

        //부모 Mask가 실제 렌더링용 Stencil Material을 다시 만들도록 갱신
        orbImageFill.SetMaterialDirty();
    }

    //PlayerEnergy의 현재 값으로 목표 표시 상태 갱신
    private void UpdateEnergyTarget()
    {
        if (playerEnergy == null) return;

        targetEnergyRatio = playerEnergy.HasOrb
            ? Mathf.Clamp01(playerEnergy.EnergyRatio)
            : 0f;

        isEnergyFull = playerEnergy.HasOrb && playerEnergy.IsFull;

        if (orbHUDGroup != null)
        {
            //보주가 없어도 빈 HUD의 프레임과 구름은 항상 표시
            orbHUDGroup.alpha = 1f;
            orbHUDGroup.interactable = false;
            orbHUDGroup.blocksRaycasts = false;
        }
    }

    //현재 기력 비율을 WaveFill Shader에 전달해 100% 이미지를 아래부터 표시
    private void UpdateEnergyFill()
    {
        if (waveFillMaterial == null || orbImageFill == null) return;

        waveFillMaterial.SetFloat(FillAmountId, displayedEnergyRatio);

        //UI Mask 아래의 Image는 Unity가 Stencil Material을 별도로 만들어 사용한다.
        //원본 Material만 수정하면 화면에 반영되지 않으므로 실제 렌더링본도 갱신한다.
        Material renderMaterial = orbImageFill.materialForRendering;

        if (renderMaterial != null && renderMaterial.HasProperty(FillAmountId))
        {
            renderMaterial.SetFloat(FillAmountId, displayedEnergyRatio);
        }
    }

    //기력이 가득 차면 전용 이펙트를 표시하고 천천히 맥동시킴
    private void UpdateFullEnergyEffect()
    {
        if (fullEnergyEffect == null) return;

        bool showFullEffect = isEnergyFull && displayedEnergyRatio >= 0.999f;
        float targetAlpha = showFullEffect ? 1f : 0f;

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
