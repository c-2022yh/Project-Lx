using UnityEngine;

public class EnergyOrb : MonoBehaviour
{

    public Transform target;
    public Vector3 offset = new Vector3(-0.6f, 0.8f, 0f); //플레이어 기준 위치
    public float followSpeed = 5f;

    [Header("Scale Settings")]
    public float normalScale = 0.5f;
    public float maxScale = 1.2f;
    public float scaleSpeed = 5f;

    [Header("Color Settings")]
    public SpriteRenderer spriteRenderer;
    public Color normalColor = Color.white;
    public Color superColor = Color.cyan;

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

        float lookDir = target.localScale.x > 0 ? -1f : 1f;
        Vector3 targetPos = target.position + new Vector3(offset.x * lookDir, offset.y, 0);

        float sinWave = Mathf.Sin(Time.time * 2f) * 0.05f;
        targetPos += new Vector3(0, sinWave, 0);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 1f / followSpeed);
        

        float currentScale = Mathf.Lerp(transform.localScale.x, targetScale, Time.deltaTime * scaleSpeed);
        transform.localScale = Vector3.one * currentScale;


    }


    public void SetEnergyRatio(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);

        targetScale = Mathf.Lerp(normalScale, maxScale, ratio);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = ratio >= 1f ? superColor : normalColor;
        }
    }


}
