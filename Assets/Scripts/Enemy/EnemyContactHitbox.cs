using UnityEngine;

//적과 플레이어가 접촉했을 때 플레이어에게 데미지를 주는 스크립트
public class EnemyContactHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    [Header("Damage Source")]
    [SerializeField] private Transform damageSource;

    private void Awake()
    {
        if (damageSource == null) damageSource = transform.root;
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        DamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        DamagePlayer(other);
    }

    private void DamagePlayer(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null) return;

        PlayerHitReaction playerHitReaction = other.GetComponentInParent<PlayerHitReaction>();

        // 대쉬 중 + 대쉬 종료 후 유예시간 동안
        // 적 접촉 데미지 무시
        if (playerHitReaction != null && playerHitReaction.IsIgnoringEnemyContact)
        {
            return;
        }

        playerHealth.TakeDamage(damage, damageSource.position);


    }
}