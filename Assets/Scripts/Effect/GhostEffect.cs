using UnityEngine;

//잔상 이펙트 처리 스크립트
public class GhostEffect : MonoBehaviour
{
    private SpriteRenderer sr;
    private float alpha;
    [SerializeField] private float fadeSpeed = 2f;

    public void Init(Vector3 pos, bool flipX)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        //위치 받아오기
        transform.position = pos;

        //방향 받아오기
        sr.flipX = flipX;

        // 알파값 조절
        alpha = 1f;
        sr.color = new Color(1f, 1f, 1f, 0.5f); 
        
    }
    private void Update()
    {
        alpha -= Time.deltaTime * fadeSpeed;
        sr.color = new Color(1f, 1f, 1f, alpha);

        if (alpha <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}