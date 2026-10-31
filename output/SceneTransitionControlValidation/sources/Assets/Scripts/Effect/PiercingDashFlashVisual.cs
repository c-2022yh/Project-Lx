using UnityEngine;

/// <summary>White flash presentation; never moves the player or changes the attack hitbox.</summary>
public sealed class PiercingDashFlashVisual : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material trailMaterial;
    [SerializeField] private Material starlightMaterial;
    [SerializeField] private Material shardMaterial;
    [SerializeField] private string sortingLayer = "Effects";
    [SerializeField] private int sortingOrder = 5;

    [Header("Departure Flash")]
    [SerializeField, Min(0.01f)] private float flashDuration = 0.055f;
    [SerializeField, Min(0f)] private float outlineIntensity = 1.6f;
    [SerializeField, Min(0.5f)] private float outlinePixels = 3f;
    [SerializeField, Range(0, 12)] private int rayCount = 6;
    [SerializeField, Min(0.01f)] private float rayLength = 0.8f;
    [SerializeField, Min(0.005f)] private float rayWidth = 0.055f;

    [Header("Original Trail Flash")]
    [Tooltip("Optional original sprite frames for the bright and fading phases.")]
    [SerializeField] private Sprite openingTrailSprite;
    [SerializeField] private Sprite fullTrailSprite;
    [SerializeField] private Sprite fadingTrailSprite;
    [SerializeField, Min(0f)] private float trailFlashIntensity = 3f;
    [SerializeField, Min(0f)] private float afterglowIntensity = 0.7f;
    [SerializeField, Min(0.01f)] private float afterglowDuration = 0.2f;
    [SerializeField, Min(0.001f)] private float revealSoftness = 0.12f;
    [SerializeField, Min(1f)] private float trailAlphaPower = 1.4f;
    [Tooltip("Thickens the white cut in texture pixels without changing its angle or stretching the sprite.")]
    [SerializeField, Min(0f)] private float trailCoreWidthPixels = 8f;
    [SerializeField, Min(0f)] private float trailEdgeGlow = 0.65f;
    [SerializeField, Min(0.5f)] private float trailEdgePixels = 2f;
    [SerializeField, Min(0.01f)] private float finishFlashDuration = 0.035f;
    [SerializeField, Min(0f)] private float finishFlashIntensity = 4f;
    [SerializeField, Min(0f)] private float finishHoldDuration = 0.04f;
    [SerializeField, Min(0.01f)] private float slashFadeDuration = 0.11f;

    [Header("One Shot Light Shards")]
    [Tooltip("Maximum shards for a full-length dash; shorter paths emit proportionally fewer.")]
    [SerializeField, Range(0, 64)] private int shardCount = 24;
    [SerializeField, Min(0.01f)] private float shardSize = 0.09f;
    [SerializeField, Min(0f)] private float shardSpreadHeight = 0.3f;
    [Tooltip("Total upward rise of the diagonal slash, in world units. Match the main sprite.")]
    [SerializeField] private float shardPathRise = 1.1f;
    [SerializeField, Min(0f)] private float shardSpeed = 2.4f;
    [SerializeField, Min(0.01f)] private float shardLifetime = 0.2f;

    public PiercingDashSkillData Skill { get; private set; }
    public float RevealedDistance { get; private set; }
    private Player owner;
    private SpriteRenderer trail;
    private SpriteRenderer outline;
    private LineRenderer[] rays;
    private ParticleSystem shards;
    private MaterialPropertyBlock properties;
    private Vector3 start;
    private Vector3 departureCenter;
    private float playerStartX;
    private float direction;
    private float distance;
    private float travelDuration;
    private float age;
    private float lifetime;
    private int emittedShards;
    private int shardBudget;
    private bool travelling;
    private bool completed;
    private float finishedAt = -1f;

    public void Begin(Player player, PiercingDashSkillData skill, SpriteRenderer originalTrail, Vector3 pathStart, float facing, float actualDistance, float duration)
    {
        owner = player;
        Skill = skill;
        if (owner == null || trailMaterial == null || starlightMaterial == null) { Destroy(gameObject); return; }
        properties = new MaterialPropertyBlock();
        start = pathStart;
        playerStartX = owner.transform.position.x;
        direction = facing >= 0f ? 1f : -1f;
        distance = Mathf.Max(0f, actualDistance);
        shardBudget = Mathf.RoundToInt(shardCount * Mathf.Clamp01(distance / Mathf.Max(0.001f, skill.dashDistance)));
        travelDuration = Mathf.Max(0.001f, duration);
        lifetime = Mathf.Max(travelDuration + flashDuration + afterglowDuration, flashDuration + shardLifetime);
        departureCenter = owner.sr != null ? owner.sr.bounds.center : owner.transform.position;
        // Parent only for per-player cancellation lookup. The effect's world origin stays fixed.
        transform.SetParent(owner.transform, true);
        transform.position = start;
        trail = originalTrail;
        if (openingTrailSprite == null) openingTrailSprite = trail != null ? trail.sprite : null;
        travelling = true;
        if (trail != null && distance > 0f)
        {
            if (openingTrailSprite != null) trail.sprite = openingTrailSprite;
            // Fit the visual sprite, including wall-shortened casts, without touching attack data.
            Vector3 scale = trail.transform.localScale;
            // Use exactly one reflection. SpriteRenderer and shader flipping together can cancel out.
            scale.x = Mathf.Abs(scale.x) * direction;
            if (trail.flipY) scale.y = -scale.y;
            trail.flipX = false;
            trail.flipY = false;
            if (trail.bounds.size.x > 0.001f) scale.x *= distance / trail.bounds.size.x;
            trail.transform.localScale = scale;
            trail.transform.position += Vector3.right * (start.x + direction * distance * 0.5f - trail.bounds.center.x);
            trail.sharedMaterial = trailMaterial;
        }
        if (owner.sr != null)
        {
            var child = new GameObject("Dash Pure White Player Outline");
            child.layer = owner.sr.gameObject.layer;
            child.transform.SetParent(owner.sr.transform, false);
            outline = child.AddComponent<SpriteRenderer>();
            outline.sharedMaterial = starlightMaterial;
        }
        rays = new LineRenderer[rayCount];
        for (int i = 0; i < rays.Length; i++)
        {
            var child = new GameObject("Departure Light Ray");
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            line.sharedMaterial = starlightMaterial;
            line.sortingLayerName = sortingLayer;
            line.sortingOrder = sortingOrder;
            rays[i] = line;
        }
        shards = CreateShards();
        Draw();
    }

    public void FinishTravel()
    {
        if (!travelling) return;
        AdvanceReveal();
        travelling = false;
        completed = true;
        finishedAt = age;
        EmitFinishShards();
        lifetime = age + Mathf.Max(shardLifetime, finishHoldDuration + slashFadeDuration) + 0.03f;
        Draw();
    }

    public void CancelTravel()
    {
        if (!travelling) return;
        // A cancelled dash fades only its committed cut; completion shards must not fire.
        AdvanceReveal();
        travelling = false;
        finishedAt = age;
        lifetime = age + slashFadeDuration + 0.03f;
        Draw();
    }

    private void AdvanceReveal()
    {
        if (!travelling || owner == null) return;
        RevealedDistance = Mathf.Max(RevealedDistance, Mathf.Clamp((owner.transform.position.x - playerStartX) * direction, 0f, distance));
    }

    private void EmitFinishShards()
    {
        int count = distance > 0.001f ? Mathf.FloorToInt(shardBudget * RevealedDistance / distance) : 0;
        for (int i = 0; i < count; i++)
        {
            float along = RevealedDistance * (i + 0.5f) / Mathf.Max(1, count);
            float angle = (i + Random.value * 0.6f) * Mathf.PI * 2f / Mathf.Max(1, count);
            Vector3 velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * shardSpeed * Random.Range(0.65f, 1.2f);
            var emit = new ParticleSystem.EmitParams
            {
                position = start + new Vector3(direction * along, shardPathRise * (along / distance - 0.5f) + Random.Range(-shardSpreadHeight, shardSpreadHeight), 0f),
                velocity = velocity,
                rotation = Random.Range(-180f, 180f),
                startSize = shardSize * Random.Range(0.65f, 1.2f)
            };
            shards.Emit(emit, 1);
            emittedShards++;
        }
    }

    private ParticleSystem CreateShards()
    {
        var child = new GameObject("Dash White Shards (One Shot)");
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);
        var ps = child.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = shardLifetime;
        main.startSpeed = 0f;
        main.startSize = shardSize;
        main.maxParticles = 64;
        var emission = ps.emission; emission.enabled = false;
        var shape = ps.shape; shape.enabled = false;
        var color = ps.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var size = ps.sizeOverLifetime; size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = shardMaterial != null ? shardMaterial : starlightMaterial;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;
        SetLight(renderer, 1, 1f);
        ps.Play();
        return ps;
    }

    private void Update()
    {
        if (properties == null) return;
        age += Time.deltaTime;
        if (age >= lifetime)
        {
            if (trail != null) trail.enabled = false;
            Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        if (properties == null) return;
        transform.position = start;
        AdvanceReveal();
        Draw();
    }

    private void Draw()
    {
        float flash = Mathf.Pow(Mathf.Clamp01(1f - age / flashDuration), 1.5f);
        if (outline != null && owner != null && owner.sr != null)
        {
            SpriteRenderer source = owner.sr;
            outline.sprite = source.sprite;
            outline.enabled = source.enabled && flash > 0f;
            outline.sortingLayerID = source.sortingLayerID;
            outline.sortingOrder = source.sortingOrder + 1;
            outline.transform.localScale = new Vector3(source.flipX ? -1f : 1f, source.flipY ? -1f : 1f, 1f);
            SetLight(outline, 2, flash * outlineIntensity * source.color.a);
            if (source.sprite != null)
            {
                Texture2D texture = source.sprite.texture;
                outline.GetPropertyBlock(properties);
                properties.SetTexture("_MainTex", texture);
                properties.SetVector("_MainTex_TexelSize", new Vector4(1f / texture.width, 1f / texture.height, texture.width, texture.height));
                properties.SetFloat("_EdgePixels", outlinePixels);
                outline.SetPropertyBlock(properties);
            }
        }
        for (int i = 0; i < rays.Length; i++)
        {
            float angle = (i * 360f / Mathf.Max(1, rays.Length) + 15f) * Mathf.Deg2Rad;
            Vector3 axis = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            rays[i].SetPosition(0, departureCenter + axis * 0.05f);
            rays[i].SetPosition(1, departureCenter + axis * rayLength * Mathf.Lerp(0.6f, 1.2f, 1f - flash));
            rays[i].widthMultiplier = rayWidth * flash;
            SetLight(rays[i], 0, flash * outlineIntensity);
        }
        if (trail == null || trail.sprite == null) return;
        Sprite phaseSprite = age < travelDuration * 0.35f ? openingTrailSprite
            : age < travelDuration + flashDuration ? fullTrailSprite : fadingTrailSprite;
        if (phaseSprite != null && trail.sprite != phaseSprite)
        {
            trail.sprite = phaseSprite;
            Vector3 scale = trail.transform.localScale;
            if (trail.bounds.size.x > 0.001f) scale.x *= distance / trail.bounds.size.x;
            trail.transform.localScale = scale;
            trail.transform.position += Vector3.right * (start.x + direction * distance * 0.5f - trail.bounds.center.x);
        }
        Texture2D trailTexture = trail.sprite.texture;
        trail.GetPropertyBlock(properties);
        properties.SetTexture("_MainTex", trailTexture);
        properties.SetVector("_MainTex_TexelSize", new Vector4(1f / trailTexture.width, 1f / trailTexture.height, trailTexture.width, trailTexture.height));
        properties.SetFloat("_StartX", start.x);
        properties.SetFloat("_Direction", direction);
        properties.SetFloat("_Distance", Mathf.Max(0.001f, distance));
        properties.SetFloat("_RevealDistance", RevealedDistance);
        properties.SetFloat("_RevealSoftness", revealSoftness);
        properties.SetFloat("_Age", age);
        properties.SetFloat("_TravelTime", travelDuration);
        properties.SetFloat("_FlashTime", flashDuration);
        properties.SetFloat("_AfterglowTime", afterglowDuration);
        properties.SetFloat("_FlashIntensity", trailFlashIntensity);
        properties.SetFloat("_AfterglowIntensity", afterglowIntensity);
        properties.SetFloat("_AlphaPower", trailAlphaPower);
        properties.SetFloat("_CoreWidthPixels", trailCoreWidthPixels);
        properties.SetFloat("_EdgeGlow", trailEdgeGlow);
        properties.SetFloat("_EdgePixels", trailEdgePixels);
        properties.SetFloat("_FinishedAt", finishedAt);
        properties.SetFloat("_EndFlashTime", finishFlashDuration);
        properties.SetFloat("_EndFlashIntensity", completed ? finishFlashIntensity : 0f);
        properties.SetFloat("_EndFadeTime", slashFadeDuration);
        properties.SetFloat("_EndHoldTime", completed ? finishHoldDuration : 0f);
        trail.SetPropertyBlock(properties);
    }

    private void SetLight(Renderer renderer, int shape, float opacity)
    {
        renderer.GetPropertyBlock(properties);
        properties.SetFloat("_Shape", shape);
        properties.SetColor("_Tint", new Color(2f, 2f, 2f, 1f));
        properties.SetFloat("_Opacity", opacity);
        renderer.SetPropertyBlock(properties);
    }

    private void OnDestroy()
    {
        if (outline != null) Destroy(outline.gameObject);
    }
}
