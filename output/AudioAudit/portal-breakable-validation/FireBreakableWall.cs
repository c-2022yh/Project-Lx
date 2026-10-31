using UnityEngine;

//화염 공격을 받으면 파괴되는 벽 프리팹
public class FireBreakableWall : MonoBehaviour
{
    [Header("Break Settings")]
    [SerializeField, Min(1)] private int requiredHitsToBreak = 1;
    [SerializeField] private GameObject breakEffectPrefab;

    [Header("Audio")]
    [Tooltip("Plays on an accepted hit only when the object/tile survives.")]
    [SerializeField] private SoundCue hitSound = new SoundCue();
    [Tooltip("Plays once on destruction, without cutting off when the object is removed.")]
    [SerializeField] private SoundCue breakSound = new SoundCue();

    [Header("Debris")]
    [SerializeField] private bool spawnDebris = true;
    [SerializeField] private Color debrisColor = new Color(0.42f, 0.44f, 0.54f);

    private int currentHitCount;
    private bool isBroken;

    private void Awake()
    {
        //이미 부순 벽이면 다시 나오지 않게 제거
        if (FieldObjectState.IsDone(this))
        {
            isBroken = true;
            Destroy(gameObject);
        }
    }

    //화염구에 맞았을 때 호출
    public void HitWall()
    {
        if (isBroken) return;

        currentHitCount++;

        if (currentHitCount >= requiredHitsToBreak)
        {
            BreakWall();
        }
        else GameAudio.Play(hitSound, transform.position);
    }

    //벽 파괴
    private void BreakWall()
    {
        //같은 프레임에 여러 화염구가 닿아도 한 번만 파괴
        isBroken = true;
        FieldObjectState.MarkDone(this);
        GameAudio.Play(breakSound, transform.position);

        //불탄 파편·먼지 (일반 벽보다 조금 세게)
        if (spawnDebris) DebrisBurst.SpawnFrom(gameObject, debrisColor, 1.2f);

        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
