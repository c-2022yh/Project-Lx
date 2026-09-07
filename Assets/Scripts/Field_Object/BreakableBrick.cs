using UnityEngine;

//공격을 받으면 파괴되는 벽돌 프리팹
public class BreakableBrick : MonoBehaviour
{
    [Header("Break Settings")]
    [SerializeField] private int requiredHitsToBreak = 1;
    [SerializeField] private GameObject breakEffectPrefab;

    private int currentHitCount;

    //공격을 받았을 때 호출
    public void HitBrick()
    {
        currentHitCount++;

        if (currentHitCount >= requiredHitsToBreak)
        {
            BreakBrick();
        }
    }

    //벽돌 파괴
    private void BreakBrick()
    {
        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
    //나중에 그래픽 들어오면 SpriteRenderer > Draw Mode = Tiled로 설정
}