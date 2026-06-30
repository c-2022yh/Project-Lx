using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Vector2 respawnOffset = Vector2.zero;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null) return;

        Vector2 savePosition = (Vector2)transform.position + respawnOffset;
        playerHealth.SetRespawnPoint(savePosition);
    }
}