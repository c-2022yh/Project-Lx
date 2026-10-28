using UnityEngine;

//공격을 받으면 파괴되는 벽돌 프리팹
public class BreakableBrick : MonoBehaviour
{
    [Header("Break Settings")]
    [SerializeField] private int requiredHitsToBreak = 1;
    [SerializeField] private GameObject breakEffectPrefab;

    [Header("Debris")]
    [SerializeField] private bool spawnDebris = true;
    [SerializeField] private Color debrisColor = new Color(0.62f, 0.56f, 0.5f);

    private int currentHitCount;

    private void Awake()
    {
        //이미 부순 벽돌이면 다시 나오지 않게 제거
        if (FieldObjectState.IsDone(this)) Destroy(gameObject);
    }

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
        FieldObjectState.MarkDone(this);

        //파편·먼지
        if (spawnDebris) DebrisBurst.SpawnFrom(gameObject, debrisColor);

        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
    //나중에 그래픽 들어오면 SpriteRenderer > Draw Mode = Tiled로 설정
}