using UnityEngine;

public class AwakeningEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Scale")]
    [SerializeField] private float startScale = 0.2f;
    [SerializeField] private float endScale = 4f;

    [Header("Fade")]
    [SerializeField] private float duration = 1.2f;

    private float timer;
    private Color startColor;
    private bool isPlaying;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            startColor = spriteRenderer.color;

        Hide();
    }

    public void SetAndShow(Vector3 startPos)
    {
        gameObject.SetActive(true);

        timer = 0f;
        isPlaying = true;

        transform.position = startPos;
        transform.localScale = Vector3.one * startScale;

        if (spriteRenderer != null)
        {
            Color color = startColor;
            color.a = startColor.a;
            spriteRenderer.color = color;
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        timer += Time.unscaledDeltaTime;

        float ratio = Mathf.Clamp01(timer / duration);

        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, ratio);

        if (spriteRenderer != null)
        {
            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, ratio);
            spriteRenderer.color = color;
        }

        if (ratio >= 1f)
        {
            Hide();
        }
    }

    public void Hide()
    {
        isPlaying = false;
        gameObject.SetActive(false);
    }
}