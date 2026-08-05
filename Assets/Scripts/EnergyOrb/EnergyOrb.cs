using UnityEngine;

//보주 관련 스크립트
public class EnergyOrb : MonoBehaviour
{
    private Transform target;

    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(-0.6f, 0.8f, 0f);
    public float followSpeed = 5f;

    [Header("Scale Settings")]
    public float normalScale = 0.5f;

    //기력 100일 때 크기
    public float maxScale = 1.2f;

    //기력 200일 때 크기
    public float enhancedScale = 1.6f;

    public float scaleSpeed = 5f;

    [Header("Color Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    //기력 0~99
    public Color normalColor = Color.white;

    //기력 100~199
    public Color superColor = Color.cyan;

    //기력 200
    public Color enhancedColor = Color.yellow;

    private float targetScale;
    private Vector3 velocity = Vector3.zero;


    private void Awake()
    {
        targetScale = normalScale;
        transform.localScale = Vector3.one * normalScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
    }


    //플레이어를 추적 대상으로 등록
    public void Initialize(Transform newTarget)
    {
        target = newTarget;

        if (target == null) return;

        //생성 직후 플레이어 근처에 배치
        transform.position = target.position + offset;
    }


    private void Update()
    {
        FollowTarget();
        UpdateScale();
    }


    //플레이어 추적
    private void FollowTarget()
    {
        if (target == null) return;

        float lookDir = target.localScale.x > 0f ? -1f : 1f;

        Vector3 targetPos = target.position + new Vector3(
            offset.x * lookDir,
            offset.y,
            0f
        );

        //위아래 둥실거림
        float sinWave = Mathf.Sin(Time.time * 2f) * 0.05f;
        targetPos.y += sinWave;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            1f / followSpeed
        );
    }


    //현재 크기를 목표 크기로 부드럽게 변경
    private void UpdateScale()
    {
        float currentScale = Mathf.Lerp(
            transform.localScale.x,
            targetScale,
            Time.deltaTime * scaleSpeed
        );

        transform.localScale = Vector3.one * currentScale;
    }


    //현재 기력에 따라 크기와 색상 변경
    public void SetEnergy(float currentEnergy)
    {
        currentEnergy = Mathf.Max(0f, currentEnergy);

        //기력 0~100
        if (currentEnergy <= 100f)
        {
            float ratio = currentEnergy / 100f;

            targetScale = Mathf.Lerp(
                normalScale,
                maxScale,
                ratio
            );

            if (spriteRenderer != null)
            {
                spriteRenderer.color =
                    currentEnergy >= 100f
                        ? superColor
                        : normalColor;
            }

            return;
        }

        //기력 100~200
        float enhancedRatio = Mathf.InverseLerp(
            100f,
            200f,
            currentEnergy
        );

        targetScale = Mathf.Lerp(
            maxScale,
            enhancedScale,
            enhancedRatio
        );

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                currentEnergy >= 200f
                    ? enhancedColor
                    : superColor;
        }
    }
}