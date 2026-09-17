using System.Collections;
using UnityEngine;

// 적과 플레이어의 접촉만 감지
public class EnemyContactHitbox : MonoBehaviour
{
    private EnemyContactAttack enemyContactAttack;

    private void Awake()
    {
        enemyContactAttack = GetComponentInParent<EnemyContactAttack>();

        Debug.Log($"ContactAttack: {contactAttack}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryContactDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryContactDamage(other);
    }

    private void TryContactDamage(Collider2D other)
    {
        Debug.Log($"Contact Trigger: {other.name}");

        //플레이어 확인
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.Log("PlayerHealth 못 음");
            return;
        }

        //대쉬 중 접촉 데미지 무시
        PlayerHitReaction playerHitReaction = other.GetComponentInParent<PlayerHitReaction>();

        if (playerHitReaction != null && playerHitReaction.IsIgnoringEnemyContact)
        {
            return;
        }

        // 데미지를 받을 수 있는 대상
        IDamageable target = other.GetComponentInParent<IDamageable>();

        if (target == null) return;

        if (enemyContactAttack == null) return;

        // 적 중심 → 플레이어 방향
        Vector2 hitDirection =
            ((Vector2)other.transform.position -
             (Vector2)transform.root.position).normalized;

        enemyContactAttack.ContactAttack(
            target,
            hitDirection
        );
    }
}