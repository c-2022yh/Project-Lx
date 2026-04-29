using UnityEngine;

public class GhostEffect : MonoBehaviour
{
    private SpriteRenderer sr;
    private float alpha;
    [SerializeField] private float fadeSpeed = 2f;

    public void Init(Sprite currentSprite, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        //이미지 받아오기
        sr.sprite = currentSprite;

        //위치와 크기 받아오기
        transform.position = pos;
        transform.rotation = rot;
        transform.localScale = scale;

        // 알파값 조절
        alpha = 1f;
        sr.color = new Color(1f, 0.2f, 0.2f, 1f); 
    }
    private void Update()
    {
        alpha -= Time.deltaTime * fadeSpeed;
        sr.color = new Color(1f, 0.2f, 0.2f, alpha);

        if (alpha <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}