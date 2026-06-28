using UnityEngine;

public class AttackEffectHitbox : MonoBehaviour
{
    [SerializeField] private float damage = 1f; // 기본 데미지
    private float attackDirection = 1f; // 공격 방향

    // 공격 정보 설정
    public void SetAttackInfo(float damageValue, float direction)
    {
        damage = damageValue;
        attackDirection = direction >= 0f ? 1f : -1f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Attack hitbox touched: " + other.name);

        // 비밀벽 타일 처리
        SecretBreakableWall secretBreakableWall = other.GetComponentInParent<SecretBreakableWall>();

        if (secretBreakableWall != null)
        {
            Vector2 hitPoint = other.ClosestPoint(transform.position);
            secretBreakableWall.HitWallAtWorldPosition(hitPoint);
            return;
        }

        // 적 피격 처리
        Enemy enemy = other.GetComponent<Enemy>();

        if (enemy == null)
        {
            enemy = other.GetComponentInParent<Enemy>();
        }

        if (enemy == null) return;

        enemy.TakeDamage(damage, new Vector2(attackDirection, 0f));
    }
}