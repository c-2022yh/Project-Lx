using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Borrows the existing orb during the visual intro; normal following resumes afterward.
[DisallowMultipleComponent, DefaultExecutionOrder(-15)]
[RequireComponent(typeof(PlayerEnergy), typeof(PlayerAwakeningVisual))]
public sealed class PlayerAwakeningOrb : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer playerSprite;
    [Tooltip("Point above the open palm, authored for the unflipped player sprite.")]
    [SerializeField] private Transform handPoint;
    [SerializeField] private Material handFlashMaterial;
    [SerializeField] private bool enableOrbMotion = true;

    [Header("Orbit (Player Local Units)")]
    [SerializeField] private Vector2 orbitCenter = new Vector2(0f, 0.25f);
    [SerializeField] private Vector2 orbitRadius = new Vector2(1.15f, 0.9f);
    [SerializeField, Range(0.25f, 2f)] private float orbitTurns = 0.75f;
    [SerializeField, Min(0f)] private float handApproachArc = 0.25f;
    [SerializeField, Range(0.1f, 1f)] private float handOrbScale = 0.35f;
    [SerializeField, Min(0.01f)] private float returnDuration = 0.25f;

    [Header("Hand Flash")]
    [SerializeField, Min(0.01f)] private float flashDuration = 0.18f;
    [SerializeField, Min(0.1f)] private float flashSize = 0.9f;
    [SerializeField, Min(0f)] private float flashIntensity = 6f;
    [SerializeField, Min(0f)] private float orbBrightnessBoost = 1.5f;
    [SerializeField, Min(0f)] private float flashLightIntensity = 1.5f;
    [SerializeField, Min(0.1f)] private float flashLightRadius = 1.2f;

    private PlayerEnergy energy;
    private PlayerAwakeningVisual visual;
    private EnergyOrb controlledOrb;
    private OrbFollower follower;
    private SpriteRenderer orbSprite;
    private Color originalColor;
    private int originalLayer, originalOrder;
    private Vector3 startLocal;
    private Vector3 originalScale, returnLocal;
    private bool returning;
    private float returnStartedAt;
    private float startAngle, direction, lastElapsed;
    private MeshRenderer flash;
    private Mesh flashMesh;
    private Light2D flashLight;
    private MaterialPropertyBlock properties;
    private static readonly int ColorId = Shader.PropertyToID("_FlashColor");
    private static readonly int IntensityId = Shader.PropertyToID("_FlashIntensity");

    private void Awake()
    {
        energy = GetComponent<PlayerEnergy>();
        visual = GetComponent<PlayerAwakeningVisual>();
        if (playerSprite == null) playerSprite = GetComponentInChildren<SpriteRenderer>();
        CreateFlash();
    }

    private void LateUpdate()
    {
        EnergyOrb orb = energy.CurrentOrb;
        float elapsed = visual.VisualIntroElapsed;
        bool active = enableOrbMotion && visual.UsesIntroSequence && visual.IsVisualActive
            && elapsed < visual.VisualIntroDuration && handPoint != null && playerSprite != null
            && orb != null && orb.isActiveAndEnabled;
        if (!active)
        {
            if (enableOrbMotion && visual.UsesIntroSequence && visual.IsVisualActive
                && elapsed >= visual.VisualIntroDuration && controlledOrb != null && controlledOrb == orb
                && orb.isActiveAndEnabled && follower != null && follower.IsPresentationControlledBy(this)) ReturnOrb();
            else ReleaseOrb();
            return;
        }
        if (controlledOrb != orb || elapsed < lastElapsed)
        {
            ReleaseOrb();
            if (!BeginOrb(orb)) return;
        }
        if (follower == null || !follower.IsPresentationControlledBy(this)) { ReleaseOrb(); return; }
        lastElapsed = elapsed;
        float chargeEnd = visual.IntroChargeEnd;
        float releaseEnd = visual.IntroReleaseEnd;
        orbSprite.transform.localScale = originalScale * Mathf.Lerp(1f, handOrbScale,
            Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(0.001f, chargeEnd)));
        Vector3 hand = GetHandPosition();
        bool behind = false;
        if (elapsed < chargeEnd)
        {
            float u = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, chargeEnd));
            Vector3 local = OrbitPosition(u);
            orb.transform.position = transform.TransformPoint(local);
            behind = Mathf.Sin(startAngle + direction * orbitTurns * Mathf.PI * 2f * u) > 0f;
        }
        else
        {
            float u = Mathf.SmoothStep(0f, 1f, (elapsed - chargeEnd) / Mathf.Max(0.001f, releaseEnd - chargeEnd));
            Vector3 from = transform.TransformPoint(OrbitPosition(1f));
            Vector3 control = (from + hand) * 0.5f + transform.up * handApproachArc;
            orb.transform.position = (1f-u)*(1f-u)*from + 2f*(1f-u)*u*control + u*u*hand;
        }
        orbSprite.sortingLayerID = playerSprite.sortingLayerID;
        orbSprite.sortingOrder = playerSprite.sortingOrder + (behind ? -2 : 2);
        float arrival = Mathf.Clamp01((elapsed - chargeEnd) / Mathf.Max(0.001f, releaseEnd-chargeEnd));
        float flashStrength = visual.IntroFlashStrength * Mathf.SmoothStep(0f, 1f, (arrival-0.7f)/0.3f)
            * (1f-Mathf.Clamp01(Mathf.Max(0f, elapsed-releaseEnd)/Mathf.Max(0.01f, flashDuration)));
        Color tint = originalColor;
        float boost = 1f + orbBrightnessBoost * flashStrength;
        tint.r *= boost; tint.g *= boost; tint.b *= boost;
        orbSprite.color = tint;
        UpdateFlash(hand, flashStrength);
    }

    private bool BeginOrb(EnergyOrb orb)
    {
        if (orb.Follower == null || orb.SpriteRenderer == null || !orb.Follower.TryBeginPresentation(this)) return false;
        controlledOrb = orb;
        follower = orb.Follower;
        orbSprite = orb.SpriteRenderer;
        originalColor = orbSprite.color;
        originalScale = orbSprite.transform.localScale;
        originalLayer = orbSprite.sortingLayerID;
        originalOrder = orbSprite.sortingOrder;
        startLocal = transform.InverseTransformPoint(orb.transform.position);
        direction = playerSprite.flipX ? -1f : 1f;
        Vector3 relative = startLocal - (Vector3)orbitCenter;
        startAngle = Mathf.Atan2(relative.y / Mathf.Max(0.01f, orbitRadius.y), relative.x / Mathf.Max(0.01f, orbitRadius.x));
        return true;
    }

    private void ReturnOrb()
    {
        if (!returning)
        {
            returning = true;
            returnStartedAt = Time.time;
            returnLocal = transform.InverseTransformPoint(controlledOrb.transform.position);
        }
        float u = Mathf.SmoothStep(0f, 1f, (Time.time-returnStartedAt)/Mathf.Max(0.01f, returnDuration));
        controlledOrb.transform.position = Vector3.Lerp(transform.TransformPoint(returnLocal), follower.FollowPosition, u);
        orbSprite.transform.localScale = originalScale * Mathf.Lerp(handOrbScale, 1f, u);
        orbSprite.color = originalColor;
        flash.enabled = false; flashLight.enabled = false;
        if (u >= 1f) ReleaseOrb();
    }

    private Vector3 OrbitPosition(float u)
    {
        float angle = startAngle + direction * orbitTurns * Mathf.PI * 2f * u;
        Vector3 radius = new Vector3(Mathf.Max(0.01f, orbitRadius.x), Mathf.Max(0.01f, orbitRadius.y), 0f);
        Vector3 ellipse = (Vector3)orbitCenter + new Vector3(Mathf.Cos(angle)*radius.x, Mathf.Sin(angle)*radius.y, startLocal.z);
        Vector3 initialEllipse = (Vector3)orbitCenter + new Vector3(Mathf.Cos(startAngle)*radius.x, Mathf.Sin(startAngle)*radius.y, startLocal.z);
        return ellipse + (startLocal-initialEllipse) * (1f-u);
    }

    private Vector3 GetHandPosition()
    {
        Vector3 local = playerSprite.transform.InverseTransformPoint(handPoint.position);
        if (playerSprite.flipX) local.x = -local.x;
        if (playerSprite.flipY) local.y = -local.y;
        return playerSprite.transform.TransformPoint(local);
    }

    private void ReleaseOrb()
    {
        if (orbSprite != null)
        {
            orbSprite.color = originalColor;
            orbSprite.transform.localScale = originalScale;
            orbSprite.sortingLayerID = originalLayer;
            orbSprite.sortingOrder = originalOrder;
        }
        if (follower != null) follower.EndPresentation(this);
        controlledOrb = null; follower = null; orbSprite = null;
        lastElapsed = 0f;
        returning = false;
        if (flash != null) flash.enabled = false;
        if (flashLight != null) flashLight.enabled = false;
    }

    private void CreateFlash()
    {
        var holder = new GameObject("Awakening Hand Flash (Runtime)");
        holder.layer = playerSprite != null ? playerSprite.gameObject.layer : gameObject.layer;
        holder.transform.SetParent(transform, false);
        flashMesh = new Mesh { name = "Awakening Hand Flash Quad" };
        flashMesh.vertices = new[] { new Vector3(-0.5f,-0.5f,0),new Vector3(0.5f,-0.5f,0),new Vector3(0.5f,0.5f,0),new Vector3(-0.5f,0.5f,0) };
        flashMesh.uv = new[] { Vector2.zero,Vector2.right,Vector2.one,Vector2.up };
        flashMesh.triangles = new[] { 0,2,1,0,3,2 };flashMesh.RecalculateBounds();
        holder.AddComponent<MeshFilter>().sharedMesh = flashMesh;
        flash = holder.AddComponent<MeshRenderer>();
        flash.sharedMaterial = handFlashMaterial;
        flash.shadowCastingMode = ShadowCastingMode.Off;
        flash.receiveShadows = false;
        flash.enabled = false;
        flashLight = holder.AddComponent<Light2D>();
        flashLight.lightType = Light2D.LightType.Point;
        flashLight.targetSortingLayers = playerSprite != null
            ? new[] { playerSprite.sortingLayerID, SortingLayer.NameToID("Ground") }
            : new[] { SortingLayer.NameToID("Ground") };
        flashLight.enabled = false;
        properties = new MaterialPropertyBlock();
    }

    private void UpdateFlash(Vector3 hand, float strength)
    {
        flash.transform.position = hand;
        flash.transform.localScale = Vector3.one * flashSize;
        flash.sortingLayerID = playerSprite.sortingLayerID;
        flash.sortingOrder = playerSprite.sortingOrder + 3;
        properties.SetColor(ColorId, visual.CurrentGlowColor);
        properties.SetFloat(IntensityId, flashIntensity * strength);
        flash.SetPropertyBlock(properties);
        flash.enabled = handFlashMaterial != null && strength > 0.001f;
        flashLight.color = visual.CurrentGlowColor;
        flashLight.intensity = flashLightIntensity * strength;
        // Radius compensates for the flash mesh's local scale.
        flashLight.pointLightOuterRadius = flashLightRadius / Mathf.Max(0.01f, flashSize);
        flashLight.enabled = strength > 0.001f;
    }

    private void OnDisable() => ReleaseOrb();
    private void OnDestroy()
    {
        ReleaseOrb();
        if (flash != null) Destroy(flash.gameObject);
        if (flashMesh != null) Destroy(flashMesh);
    }

    private void OnDrawGizmosSelected()
    {
        if (playerSprite == null || handPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(GetHandPosition(), 0.12f);
    }
}
