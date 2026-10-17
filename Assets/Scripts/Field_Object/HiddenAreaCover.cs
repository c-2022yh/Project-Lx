using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class HiddenAreaCover : MonoBehaviour
{
    [Header("Cover")]
    [Tooltip("Sprites that cover the hidden area. Use an Unlit material and a foreground sorting layer.")]
    [SerializeField] private SpriteRenderer[] coverRenderers;

    [Range(0f, 1f)]
    [SerializeField] private float hiddenAlpha = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float revealedAlpha = 0f;

    [Header("Fade (seconds)")]
    [Min(0f)]
    [SerializeField] private float revealDuration = 0.3f;
    [Min(0f)]
    [SerializeField] private float hideDuration = 0.5f;

    private readonly HashSet<Collider2D> playerContacts = new HashSet<Collider2D>();
    private float currentAlpha;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        if (!GetComponent<Collider2D>().isTrigger)
        {
            Debug.LogWarning("HiddenAreaCover requires Is Trigger on its Collider2D.", this);
        }
    }

    private void OnEnable()
    {
        playerContacts.Clear();
        currentAlpha = hiddenAlpha;
        ApplyAlpha();
    }

    private void OnDisable()
    {
        playerContacts.Clear();
        currentAlpha = hiddenAlpha;
        ApplyAlpha();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrackPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Also recover contacts when enabled while the player is already inside.
        TrackPlayer(other);
    }

    private void TrackPlayer(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() != null)
        {
            playerContacts.Add(other);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        playerContacts.Remove(other);
    }

    private void Update()
    {
        // Disabled or destroyed colliders may disappear without an exit callback.
        playerContacts.RemoveWhere(IsInvalidContact);

        bool revealed = playerContacts.Count > 0;
        float targetAlpha = revealed ? revealedAlpha : hiddenAlpha;
        float duration = revealed ? revealDuration : hideDuration;
        float alphaRange = Mathf.Abs(hiddenAlpha - revealedAlpha);

        currentAlpha = duration <= 0f
            ? targetAlpha
            : Mathf.MoveTowards(currentAlpha, targetAlpha, alphaRange * Time.deltaTime / duration);

        ApplyAlpha();
    }

    private static bool IsInvalidContact(Collider2D contact)
    {
        return contact == null || !contact.enabled || !contact.gameObject.activeInHierarchy;
    }

    private void ApplyAlpha()
    {
        if (coverRenderers == null) return;

        foreach (SpriteRenderer cover in coverRenderers)
        {
            if (cover == null) continue;

            Color color = cover.color;
            color.a = currentAlpha;
            cover.color = color;
        }
    }
}
