using System.Collections;
using UnityEngine;

//뇌전 유물 전이 번개 이펙트
[RequireComponent(typeof(LineRenderer))]
public class ChainLightningEffect : MonoBehaviour
{
    [Header("Lightning Shape")]
    [SerializeField]
    [Min(2)]
    private int segmentCount = 8;

    [Tooltip("번개가 직선에서 얼마나 크게 흔들릴지")]
    [SerializeField]
    [Min(0f)]
    private float noiseAmount = 0.15f;

    [Header("Animation")]
    [Tooltip("번개 모양이 바뀌는 간격")]
    [SerializeField]
    [Min(0.01f)]
    private float refreshInterval = 0.03f;

    [Tooltip("번개가 화면에 남아있는 시간")]
    [SerializeField]
    [Min(0.01f)]
    private float lifeTime = 0.12f;

    private LineRenderer lineRenderer;

    private Transform startTarget;
    private Transform endTarget;

    private Vector3 startPosition;
    private Vector3 endPosition;

    private bool followTargets;


    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = segmentCount;
        lineRenderer.useWorldSpace = true;
    }


    //움직이는 두 대상 사이를 연결할 때 사용
    public void Init(Transform startTarget, Transform endTarget)
    {
        this.startTarget = startTarget;
        this.endTarget = endTarget;

        followTargets = true;

        StartCoroutine(LightningRoutine());
    }


    //고정된 두 위치 사이를 연결할 때 사용
    public void Init(Vector3 startPosition, Vector3 endPosition)
    {
        this.startPosition = startPosition;
        this.endPosition = endPosition;

        followTargets = false;

        StartCoroutine(LightningRoutine());
    }


    private IEnumerator LightningRoutine()
    {
        float elapsed = 0f;
        float refreshTimer = 0f;

        UpdateLightning();

        while (elapsed < lifeTime)
        {
            elapsed += Time.deltaTime;
            refreshTimer += Time.deltaTime;

            if (refreshTimer >= refreshInterval)
            {
                refreshTimer = 0f;

                UpdateLightning();
            }

            yield return null;
        }

        Destroy(gameObject);
    }


    //번개의 지그재그 모양 생성
    private void UpdateLightning()
    {
        Vector3 start;
        Vector3 end;

        if (followTargets)
        {
            if (startTarget == null || endTarget == null)
            {
                Destroy(gameObject);
                return;
            }

            start = startTarget.position;
            end = endTarget.position;
        }
        else
        {
            start = startPosition;
            end = endPosition;
        }


        Vector3 direction = end - start;

        //번개 진행 방향과 수직인 방향
        Vector3 perpendicular =
            new Vector3(
                -direction.y,
                direction.x,
                0f
            ).normalized;


        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);

            Vector3 position = Vector3.Lerp(start, end, t);

            //시작점과 끝점은 흔들지 않음
            if (i != 0 && i != segmentCount - 1)
            {
                float noise = UnityEngine.Random.Range(-noiseAmount, noiseAmount);

                position += perpendicular * noise;
            }

            lineRenderer.SetPosition(i, position);
        }

    }
}