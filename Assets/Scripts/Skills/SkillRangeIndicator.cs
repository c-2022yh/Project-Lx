using UnityEngine;

public class SkillRangeIndicator : MonoBehaviour
{
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        gameObject.SetActive(false);
    }

    //스킬 데이터로부터 설정을 받아오는 함수
    public void SetAndShow(Vector2 size, Color color, Vector3 offset)
    {
        gameObject.SetActive(true);

        transform.localScale = size;

        if (sr != null) sr.color = color;

        transform.localPosition = offset;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}