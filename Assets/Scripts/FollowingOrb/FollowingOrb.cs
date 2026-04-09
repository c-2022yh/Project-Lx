using UnityEngine;

public class FollowingOrb : MonoBehaviour
{

    public Transform target; // 따라갈 대상 (플레이어)
    public Vector3 offset = new Vector3(-1f, 1f, 0f); // 플레이어 기준 위치
    public float followSpeed = 5f;

    [Header("Scale Settings")]
    public float normalScale = 0.5f; // 평소 크기
    public float superScale = 1.2f;  // 각성 시 크기
    public float scaleSpeed = 5f;    // 커지는 속도

    private float targetScale;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        targetScale = normalScale; // 시작은 평범하게
        transform.localScale = Vector3.one * normalScale;

    }

    void Update()
    {
        if (target == null) return;

        // 1. 이동 로직 (기존과 동일)
        float lookDir = target.localScale.x > 0 ? -1f : 1f;
        Vector3 targetPos = target.position + new Vector3(offset.x * lookDir, offset.y, 0);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 1f / followSpeed);

        // 둥둥 떠있는 효과
        float sinWave = Mathf.Sin(Time.time * 2f) * 0.2f;
        transform.position += new Vector3(0, sinWave, 0);

        // 2. 크기 조절 로직 (핵심!)
        float currentScale = Mathf.Lerp(transform.localScale.x, targetScale, Time.deltaTime * scaleSpeed);
        transform.localScale = Vector3.one * currentScale;
    }


    public void SetSuperMode(bool isSuper)
    {
        targetScale = isSuper ? superScale : normalScale;
    }



}
