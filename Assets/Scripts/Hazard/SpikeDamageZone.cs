using UnityEngine;

public class SpikeDamageZone : MonoBehaviour
{
    [SerializeField] private int damage = 1;

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

        playerHealth.TakeDamage(damage, transform.position);
    }
}