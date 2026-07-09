using System.Collections.Generic;
using UnityEngine;

public class AttackEffectHitbox : MonoBehaviour
{
    //히트박스가 전달할 피해 정보
    private DamageInfo damageInfo;

    //공격 방향
    private float attackDirection = 1f;

    //이 공격 이펙트가 이미 타격한 적들
    private readonly HashSet<IDamageable> hitTargets = new();

    //공격 이펙트의 콜라이더 (실제 피격 판정)
    [SerializeField] private Collider2D hitCollider;

    private void Awake()
    {
        //콜라이더 연결
        hitCollider = GetComponent<Collider2D>();
    }

    //공격 정보 설정
    public void SetAttackInfo(DamageInfo damageInfo, float direction)
    {
        this.damageInfo = damageInfo; //데미지 정보
        attackDirection = direction >= 0f ? 1f : -1f; //공격 방향   
    }

    //공격 히트박스 해제
    public void DisableHitbox()
    {
        hitCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        //비밀벽 타일 처리
        SecretBreakableWall secretBreakableWall = other.GetComponentInParent<SecretBreakableWall>();
        if (secretBreakableWall != null)
        {
            Vector2 hitPoint = other.ClosestPoint(transform.position);
            secretBreakableWall.HitWallAtWorldPosition(hitPoint);
            return;
        }


        //공통 피격 대상 탐색
        IDamageable target = other.GetComponentInParent<IDamageable>();

        if (target == null) return;

        //같은 대상은 이 히트박스에 한 번만 피격
        if (!hitTargets.Add(target)) return;

        //피해 정보 전달
        target.TakeDamage(damageInfo, new Vector2(attackDirection, 0f));
        

    }
}