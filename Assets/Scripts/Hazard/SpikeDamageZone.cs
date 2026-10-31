using UnityEngine;

public class SpikeDamageZone : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    [Header("Audio")]
    [SerializeField] private SoundCue damageSound = new SoundCue();



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

        float before = playerHealth.CurrentHealth;
        playerHealth.TakeDamage(damage, transform.position);
        if (playerHealth.CurrentHealth < before) GameAudio.Play(damageSound, playerHealth.transform.position);
    }
}