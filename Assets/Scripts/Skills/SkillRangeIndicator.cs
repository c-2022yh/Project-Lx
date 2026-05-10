using UnityEngine;

public class SkillRangeIndicator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    void Awake()
    {
        Hide();
    }

    public void SetAndShow(Vector2 size, Color color, Vector3 startPos, float direction)
    {
        gameObject.SetActive(true);
        transform.localScale = new Vector3(size.x, size.y, 1f);

        float offsetX = (size.x / 2f) * direction;
        transform.position = new Vector3(startPos.x + offsetX, startPos.y, startPos.z);

        if (spriteRenderer != null) spriteRenderer.color = color;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}