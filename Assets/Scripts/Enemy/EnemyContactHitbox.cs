using UnityEngine;

public class EnemyContactHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    [Header("Damage Source")]
    [SerializeField] private Transform damageSource;

    private void Awake()
    {
        if (damageSource == null)
        {
            damageSource = transform.root;
        }
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
        //PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        //PlayerHitReaction playerHitReaction = other.GetComponentInParent<PlayerHitReaction>();

        //대쉬 중 + 착지 후 유예시간 동안 적 접촉 데미지 무시
        //if (playerHitReaction.IsIgnoringEnemyContact) return;

        //playerHealth.TakeDamage(damage, transform.position);
    }
}