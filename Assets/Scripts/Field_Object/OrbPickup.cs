using System.Collections.Generic;
using UnityEngine;

// Grants the player's first orb through the same interaction system as RelicChest.
[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class OrbPickup : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private string interactionText = "보주 획득";
    [Tooltip("Destroy this object after successfully granting the orb. If disabled, leave it as scenery.")]
    [SerializeField] private bool destroyOnAcquire = true;

    [Header("Audio")]
    [SerializeField] private SoundCue acquireSound = new SoundCue();

    private readonly HashSet<Collider2D> playerContacts = new HashSet<Collider2D>();
    private Player nearbyPlayer;
    private bool isAcquired;

    public string InteractionText => interactionText;

    private void Awake()
    {
        // 이미 먹은 보주면 다시 나오지 않게 한다.
        if (!FieldObjectState.IsDone(this)) return;
        isAcquired = true;
        if (destroyOnAcquire) Destroy(gameObject);
    }

    private void Reset()
    {
        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    public void Interact(Player player)
    {
        if (!isActiveAndEnabled || isAcquired || player == null || player != nearbyPlayer) return;

        PlayerEnergy energy = player.GetComponent<PlayerEnergy>();
        if (energy == null) return;
        if (energy.HasOrb)
        {
            ClearPlayer();
            return;
        }

        // Guard against repeated interaction while acquisition notifies other systems.
        isAcquired = true;
        energy.AcquireOrb();
        if (!energy.HasOrb)
        {
            isAcquired = false;
            Debug.LogWarning("[OrbPickup] Assign Orb Prefab on the player's PlayerEnergy component.", this);
            return;
        }

        FieldObjectState.MarkDone(this);
        GameAudio.Play(acquireSound, transform.position);
        ClearPlayer();
        if (destroyOnAcquire) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isAcquired || !isActiveAndEnabled) return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null || (nearbyPlayer != null && nearbyPlayer != player)) return;

        PlayerEnergy energy = player.GetComponent<PlayerEnergy>();
        if (energy == null || energy.HasOrb) return;

        nearbyPlayer = player;
        // Keep interaction available until all of the player's colliders leave.
        if (playerContacts.Add(other) && playerContacts.Count == 1)
            player.SetInteractable(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!playerContacts.Remove(other)) return;
        if (playerContacts.Count == 0) ClearPlayer();
    }

    private void OnDisable()
    {
        ClearPlayer();
    }

    private void ClearPlayer()
    {
        if (nearbyPlayer != null) nearbyPlayer.ClearInteractable(this);
        nearbyPlayer = null;
        playerContacts.Clear();
    }
}
