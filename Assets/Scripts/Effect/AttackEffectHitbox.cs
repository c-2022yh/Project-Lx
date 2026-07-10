using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackEffectHitbox : MonoBehaviour
{
    [SerializeField] private float damage = 1f; // 기본 데미지
    private float attackDirection = 1f; // 공격 방향

    //이 공격 이펙트가 이미 타격한 적들
    private readonly HashSet<Enemy> hitEnemies = new();

    //공격 이펙트의 콜라이더 (실제 피격 판정)
    [SerializeField] private Collider2D hitCollider;

    private void Awake()
    {
        //콜라이더 연결
        hitCollider = GetComponent<Collider2D>();
    }

    //공격 정보 설정
    public void SetAttackInfo(float damageValue, float direction)
    {
        damage = damageValue; //데미지
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

        //적 피격 처리
        Enemy enemy = other.GetComponentInParent<Enemy>();

        if (enemy == null) return;
        //같은 적은 이 공격 이펙트에 한 번만 피격
        if (!hitEnemies.Add(enemy)) return;

        enemy.TakeDamage(damage, new Vector2(attackDirection, 0f));
    }
}