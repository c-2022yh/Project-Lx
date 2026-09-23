using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class OrbVisual : MonoBehaviour
{
    [Header("Scale Settings")]
    [SerializeField] private float normalScale = 0.5f;
    [SerializeField] private float maxScale = 1.2f;
    [SerializeField] private float enhancedScale = 1.6f;

    [SerializeField, Min(0f)]
    private float scaleSpeed = 5f;

    [Header("Color Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color chargedColor = Color.cyan;
    [SerializeField] private Color enhancedColor = Color.yellow;

    private SpriteRenderer spriteRenderer;
    private float targetScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        targetScale = normalScale;

        transform.localScale =
            Vector3.one * normalScale;

        spriteRenderer.color = normalColor;
    }

    private void Update()
    {
        UpdateScale();
    }

    public void SetEnergy(float currentEnergy)
    {
        currentEnergy = Mathf.Max(0f, currentEnergy);

        if (currentEnergy <= 100f)
        {
            float ratio = currentEnergy / 100f;

            targetScale = Mathf.Lerp(
                normalScale,
                maxScale,
                ratio
            );

            spriteRenderer.color =
                currentEnergy >= 100f
                    ? chargedColor
                    : normalColor;

            return;
        }

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

        spriteRenderer.color =
            currentEnergy >= 200f
                ? enhancedColor
                : chargedColor;
    }

    private void UpdateScale()
    {
        float currentScale = Mathf.Lerp(
            transform.localScale.x,
            targetScale,
            Time.deltaTime * scaleSpeed
        );

        transform.localScale =
            Vector3.one * currentScale;
    }
}