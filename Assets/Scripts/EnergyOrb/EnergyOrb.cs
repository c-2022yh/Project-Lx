using System.Collections;
using System.Diagnostics;
using UnityEngine;

// 보주 관련 스크립트
public class EnergyOrb : MonoBehaviour
{
    private Transform target;

    public Vector3 offset = new Vector3(-0.6f, 0.8f, 0f);
    public float followSpeed = 5f;

    [Header("Scale Settings")]
    public float normalScale = 0.5f;
    public float maxScale = 1.2f;
    public float scaleSpeed = 5f;

    [Header("Color Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    public Color normalColor = Color.white;
    public Color superColor = Color.cyan;

    private float targetScale;
    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        targetScale = normalScale;
        transform.localScale = Vector3.one * normalScale;
    }

    //target을 등록하는 함수
    public void Initialize(Transform newTarget)
    {
        target = newTarget;
        if (target == null) return;

        //생성 직후 너무 먼 곳에서 날아오지 않도록
        transform.position = target.position + offset;
    }

    private void Update()
    {
        //추적 부분만 target이 있을 때 실행
        if (target != null)
        {
            float lookDir = target.localScale.x > 0f ? -1f : 1f;

            Vector3 targetPos =target.position + new Vector3(
                    offset.x * lookDir,
                    offset.y,
                    0f
                );

            //위아래 둥실거림
            float sinWave =Mathf.Sin(Time.time * 2f) * 0.05f;
            targetPos.y += sinWave;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPos,
                ref velocity,
                1f / followSpeed
            );
        }

        //크기 변화는 target 유무와 상관없이 항상 실행
        float currentScale = Mathf.Lerp(
            transform.localScale.x,
            targetScale,
            Time.deltaTime * scaleSpeed
        );

        transform.localScale =
            Vector3.one * currentScale;
    }

    public void SetEnergyRatio(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);

        targetScale = Mathf.Lerp(
            normalScale,
            maxScale,
            ratio
        );

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                ratio >= 1f
                    ? superColor
                    : normalColor;
        }
    }
}