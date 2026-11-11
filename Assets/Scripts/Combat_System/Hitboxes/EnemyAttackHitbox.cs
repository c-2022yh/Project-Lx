using System.Collections.Generic;
using UnityEngine;

//적의 공격 판정을 담당하는 공용 히트박스
//엘리트, 보스의 근거리 공격 등에 공통 사용
public class EnemyAttackHitbox : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private LayerMask targetLayer;

    private DamageInfo damageInfo;

    //피격 시 밀려날 방향
    private Vector2 hitDirection;

    //같은 공격으로 같은 대상을 여러 번 타격하는 것 방지
    private readonly HashSet<IDamageable> hitTargets = new();

    private bool initialized;


    //공격 생성 시 필요한 정보 전달
    public void Init(DamageInfo damageInfo, Vector2 hitDirection, float lifeTime)
    {
        this.damageInfo = damageInfo;
        this.hitDirection = hitDirection.normalized;

        hitTargets.Clear();

        initialized = true;

        //공격 판정 유지시간이 끝나면 제거
        Destroy(gameObject, lifeTime);
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!initialized) return;


        //공격 대상 레이어인지 확인
        bool isTargetLayer = (targetLayer.value & (1 << other.gameObject.layer)) != 0;

        if (!isTargetLayer) return;

        //데미지를 받을 수 있는 대상인지 확인
        IDamageable target = other.GetComponent<IDamageable>();

        if (target == null) return;

        //한 공격에서 같은 대상 중복 타격 방지
        if (!hitTargets.Add(target)) return;

        //데미지 + 넉백 방향 전달
        target.TakeDamage(
            damageInfo,
            hitDirection
        );
    }
}