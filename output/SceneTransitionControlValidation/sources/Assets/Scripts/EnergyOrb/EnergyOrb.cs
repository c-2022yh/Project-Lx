using UnityEngine;

[RequireComponent(typeof(OrbFollower))]
public class EnergyOrb : MonoBehaviour
{
    [Header("Orb Components")]
    [SerializeField] private OrbLight orbLight;

    [Header("Orb Sprite")]
    [Tooltip("Renderer for the in-game orb. Defaults to the SpriteRenderer on this object.")]
    [SerializeField] private SpriteRenderer orbSpriteRenderer;

    private OrbFollower follower;
    private Sprite defaultSprite;
    private PlayerRelicManager relicManager;

    public OrbFollower Follower => follower;
    public SpriteRenderer SpriteRenderer => orbSpriteRenderer;

    private void Awake()
    {
        follower = GetComponent<OrbFollower>();
        if (orbSpriteRenderer == null) orbSpriteRenderer = GetComponent<SpriteRenderer>();
        if (orbSpriteRenderer == null) orbSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (orbSpriteRenderer != null) defaultSprite = orbSpriteRenderer.sprite;
    }

    public void Initialize(Transform target)
    {
        UnsubscribeRelics();
        follower.Initialize(target);
        relicManager = target != null ? target.GetComponent<PlayerRelicManager>() : null;
        if (isActiveAndEnabled) SubscribeRelics();
        RefreshSprite();
    }

    private void OnEnable()
    {
        SubscribeRelics();
        RefreshSprite();
    }

    private void OnDisable()
    {
        UnsubscribeRelics();
    }

    private void SubscribeRelics()
    {
        if (relicManager == null) return;
        relicManager.OnRelicsChanged -= RefreshSprite;
        relicManager.OnRelicsChanged += RefreshSprite;
    }

    private void UnsubscribeRelics()
    {
        if (relicManager != null) relicManager.OnRelicsChanged -= RefreshSprite;
    }

    private void RefreshSprite()
    {
        if (orbSpriteRenderer == null) return;

        RelicData orbRelic = relicManager != null ? relicManager.EquippedOrbRelic : null;
        orbSpriteRenderer.sprite = orbRelic != null && orbRelic.OrbSprite != null
            ? orbRelic.OrbSprite
            : defaultSprite;
    }

    public void SetLight(float intensity, float radius, Color color)
    {
        orbLight.SetLight(intensity, radius, color);
    }
}
