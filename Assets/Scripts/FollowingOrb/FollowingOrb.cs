using UnityEngine;

public class FollowingOrb : MonoBehaviour
{

    public Transform target;
    public Vector3 offset = new Vector3(-0.6f, 0.8f, 0f); //플레이어 기준 위치
    public float followSpeed = 5f;

    [Header("Scale Settings")]
    public float normalScale = 0.5f;
    public float superScale = 1.2f;  
    public float scaleSpeed = 5f;

    private float targetScale;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        targetScale = normalScale;
        transform.localScale = Vector3.one * normalScale;

    }

    void Update()
    {
        if (target == null) return;

        //이동 로직 (기존과 동일)
        float lookDir = target.localScale.x > 0 ? -1f : 1f;
        Vector3 targetPos = target.position + new Vector3(offset.x * lookDir, offset.y, 0);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 1f / followSpeed);

        //둥둥 떠있는 효과
        float sinWave = Mathf.Sin(Time.time * 2f) * 0.02f;
        transform.position += new Vector3(0, sinWave, 0);

        float currentScale = Mathf.Lerp(transform.localScale.x, targetScale, Time.deltaTime * scaleSpeed);
        transform.localScale = Vector3.one * currentScale;
    }


    public void SetSuperMode(bool isSuper)
    {
       
    }



}
