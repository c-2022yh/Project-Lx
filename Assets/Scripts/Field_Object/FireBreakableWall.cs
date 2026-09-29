using UnityEngine;

//화염 공격을 받으면 파괴되는 벽 프리팹
public class FireBreakableWall : MonoBehaviour
{
    [Header("Break Settings")]
    [SerializeField, Min(1)] private int requiredHitsToBreak = 1;
    [SerializeField] private GameObject breakEffectPrefab;

    private int currentHitCount;
    private bool isBroken;

    //화염구에 맞았을 때 호출
    public void HitWall()
    {
        if (isBroken) return;

        currentHitCount++;

        if (currentHitCount >= requiredHitsToBreak)
        {
            BreakWall();
        }
    }

    //벽 파괴
    private void BreakWall()
    {
        //같은 프레임에 여러 화염구가 닿아도 한 번만 파괴
        isBroken = true;

        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
