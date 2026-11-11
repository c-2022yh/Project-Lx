using System.Collections;
using UnityEngine;

//적 피격 시 재생되는 3단계 타격 이펙트
public class EnemyHitEffect : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private SpriteRenderer hit1;
    [SerializeField] private SpriteRenderer hit2;
    [SerializeField] private SpriteRenderer hit3;

    [Header("Delay")]
    [SerializeField] private float hit1Delay = 0f;
    [SerializeField] private float hit2Delay = 0.03f;
    [SerializeField] private float hit3Delay = 0.07f;

    [Header("Duration")]
    [SerializeField] private float hit1Duration = 0.10f;
    [SerializeField] private float hit2Duration = 0.12f;
    [SerializeField] private float hit3Duration = 0.15f;

    [Header("Scale")]
    [SerializeField]
    private Vector2 hit1Scale =
        new Vector2(0.4f, 0.9f);

    [SerializeField]
    private Vector2 hit2Scale =
        new Vector2(0.6f, 1.1f);

    [SerializeField]
    private Vector2 hit3Scale =
        new Vector2(0.8f, 1.25f);

    [Header("Random")]
    [SerializeField] private float randomRotation = 15f;
    [SerializeField] private float randomPositionOffset = 0.05f;


    private void Awake()
    {
        HideAll();
    }


    private void OnEnable()
    {
        StopAllCoroutines();
        HideAll();

        //매번 조금씩 다른 위치에서 재생
        transform.position += new Vector3(
            UnityEngine.Random.Range(
                -randomPositionOffset,
                randomPositionOffset
            ),
            UnityEngine.Random.Range(
                -randomPositionOffset,
                randomPositionOffset
            ),
            0f
        );

        StartCoroutine(
            PlayStage(
                hit1,
                hit1Delay,
                hit1Duration,
                hit1Scale.x,
                hit1Scale.y,
                8f
            )
        );

        StartCoroutine(
            PlayStage(
                hit2,
                hit2Delay,
                hit2Duration,
                hit2Scale.x,
                hit2Scale.y,
                -10f
            )
        );

        StartCoroutine(
            PlayStage(
                hit3,
                hit3Delay,
                hit3Duration,
                hit3Scale.x,
                hit3Scale.y,
                6f
            )
        );

        float totalLife = Mathf.Max(
            hit1Delay + hit1Duration,
            hit2Delay + hit2Duration,
            hit3Delay + hit3Duration
        );

        StartCoroutine(
            DestroyAfterRoutine(totalLife + 0.02f)
        );
    }


    //스프라이트 하나의 확대, 회전, 페이드 처리
    private IEnumerator PlayStage(
        SpriteRenderer spriteRenderer,
        float delay,
        float duration,
        float startScale,
        float endScale,
        float rotationAmount
    )
    {
        if (spriteRenderer == null)
            yield break;

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        spriteRenderer.gameObject.SetActive(true);

        float startAngle =
            UnityEngine.Random.Range(
                -randomRotation,
                randomRotation
            );

        float endAngle =
            startAngle + rotationAmount;

        Color baseColor = spriteRenderer.color;
        baseColor.a = 1f;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(timer / duration);

            //처음에는 빠르게 커지고 나중에는 천천히
            float easedTime =
                1f - Mathf.Pow(
                    1f - normalizedTime,
                    3f
                );

            float currentScale =
                Mathf.Lerp(
                    startScale,
                    endScale,
                    easedTime
                );

            spriteRenderer.transform.localScale =
                Vector3.one * currentScale;

            float currentAngle =
                Mathf.Lerp(
                    startAngle,
                    endAngle,
                    normalizedTime
                );

            spriteRenderer.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    currentAngle
                );

            //초반에는 선명하고 후반에 빠르게 사라짐
            float alpha = 1f;

            const float fadeStart = 0.35f;

            if (normalizedTime > fadeStart)
            {
                alpha = 1f -
                    (
                        normalizedTime - fadeStart
                    ) /
                    (
                        1f - fadeStart
                    );
            }

            spriteRenderer.color = new Color(
                baseColor.r,
                baseColor.g,
                baseColor.b,
                Mathf.Clamp01(alpha)
            );

            yield return null;
        }

        spriteRenderer.gameObject.SetActive(false);
    }


    private IEnumerator DestroyAfterRoutine(
        float delay
    )
    {
        yield return new WaitForSeconds(delay);

        Destroy(gameObject);
    }


    private void HideAll()
    {
        PrepareSprite(hit1);
        PrepareSprite(hit2);
        PrepareSprite(hit3);
    }


    private void PrepareSprite(
        SpriteRenderer spriteRenderer
    )
    {
        if (spriteRenderer == null)
            return;

        Color color = spriteRenderer.color;
        color.a = 1f;

        spriteRenderer.color = color;
        spriteRenderer.transform.localScale =
            Vector3.zero;

        spriteRenderer.transform.localRotation =
            Quaternion.identity;

        spriteRenderer.gameObject.SetActive(false);
    }
}