using UnityEngine;
using UnityEngine.UI;

//플레이어의 현재 기력에 따라 보주 HUD를 갱신하는 스크립트
public class OrbHUD : MonoBehaviour
{
    private PlayerEnergy playerEnergy;

    [Header("Energy Fill")]
    [SerializeField] private Image blueBackgroundFill;
    [SerializeField] private Image orbImageFill;

    [Header("Wave")]
    [SerializeField] private RectTransform waveRoot;
    [SerializeField] private RectTransform wave1;
    [SerializeField] private RectTransform wave2;

    [SerializeField] private float emptyWaveY = -250f;
    [SerializeField] private float fullWaveY = 250f;

    [SerializeField, Min(0f)]
    private float waveMoveRange = 12f;

    [SerializeField, Min(0f)]
    private float wave1MoveSpeed = 1f;

    [SerializeField, Min(0f)]
    private float wave2MoveSpeed = 0.7f;

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

    private Vector2 wave1StartPosition;
    private Vector2 wave2StartPosition;
    private Vector2 waveRootStartPosition;

    private Vector3 fullEffectStartScale = Vector3.one;

    //플레이어 기력을 찾고 변경 이벤트를 구독
    private void Start()
    {
        playerEnergy = FindAnyObjectByType<PlayerEnergy>();

        if (playerEnergy == null)
        {
            Debug.LogError("OrbHUD: PlayerEnergy를 찾을 수 없습니다.", this);
            return;
        }

        if (waveRoot != null)
        {
            waveRootStartPosition = waveRoot.anchoredPosition;
        }

        if (wave1 != null)
        {
            wave1StartPosition = wave1.anchoredPosition;
        }

        if (wave2 != null)
        {
            wave2StartPosition = wave2.anchoredPosition;
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
    }

    private void Update()
    {
        if (playerEnergy == null) return;

        //기력 변화가 너무 딱딱하게 보이지 않도록 표시값을 부드럽게 변경
        displayedEnergyRatio = Mathf.MoveTowards(
            displayedEnergyRatio,
            targetEnergyRatio,
            fillSmoothSpeed * Time.unscaledDeltaTime
        );

        UpdateEnergyFill();
        UpdateWaves();
        UpdateFullEnergyEffect();
    }

    //PlayerEnergy의 현재 값으로 목표 표시 상태 갱신
    private void UpdateEnergyTarget()
    {
        if (playerEnergy == null) return;

        targetEnergyRatio = Mathf.Clamp01(playerEnergy.EnergyRatio);
        isEnergyFull = playerEnergy.IsFull;

        if (orbHUDGroup != null)
        {
            orbHUDGroup.alpha = playerEnergy.HasOrb ? 1f : 0f;
            orbHUDGroup.interactable = false;
            orbHUDGroup.blocksRaycasts = false;
        }
    }

    //푸른 배경과 100% 보주 이미지를 아래에서 위로 채움
    private void UpdateEnergyFill()
    {
        if (blueBackgroundFill != null)
        {
            blueBackgroundFill.fillAmount = displayedEnergyRatio;
        }

        if (orbImageFill != null)
        {
            orbImageFill.fillAmount = displayedEnergyRatio;
        }
    }

    //현재 기력의 수면 높이에서 두 물결을 서로 다르게 움직임
    private void UpdateWaves()
    {
        bool showWave = displayedEnergyRatio > 0.001f && displayedEnergyRatio < 0.999f;

        if (waveRoot != null)
        {
            waveRoot.gameObject.SetActive(showWave);
        }

        if (!showWave) return;

        float waveY = Mathf.Lerp(emptyWaveY, fullWaveY, displayedEnergyRatio);
        float time = Time.unscaledTime;

        if (waveRoot != null)
        {
            waveRoot.anchoredPosition = new Vector2(
                waveRootStartPosition.x,
                waveY
            );
        }

        if (wave1 != null)
        {
            float waveX = wave1StartPosition.x
                + Mathf.Sin(time * wave1MoveSpeed) * waveMoveRange;

            wave1.anchoredPosition = new Vector2(
                waveX,
                wave1StartPosition.y
            );
        }

        if (wave2 != null)
        {
            float waveX = wave2StartPosition.x
                + Mathf.Sin(time * wave2MoveSpeed + Mathf.PI) * waveMoveRange;

            wave2.anchoredPosition = new Vector2(
                waveX,
                wave2StartPosition.y
            );
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
