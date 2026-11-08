using UnityEngine;

/// <summary>Presentation only. The skill and its existing hitboxes own timing and damage.</summary>
public sealed class SlashSkillVisual : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Material starlightMaterial;
    [SerializeField, ColorUsage(true, true)] private Color slashColor = new Color(0.65f, 1.6f, 2.1f, 1f);
    [SerializeField, ColorUsage(true, true)] private Color finisherColor = new Color(1.8f, 1.7f, 1.1f, 1f);
    [SerializeField] private string sortingLayer = "Effects";
    [SerializeField] private int sortingOrder = 4;

    [Header("Sword Glint")]
    [SerializeField] private Vector2 preparationOffset = new Vector2(1f, 0f);
    [SerializeField, Min(0.01f)] private float preparationSize = 0.55f;
    [SerializeField, Min(0.01f)] private float preparationDuration = 0.1f;
    [SerializeField, Range(0, 32)] private int gatheringCount = 10;
    [SerializeField, Min(0.01f)] private float gatheringRadius = 0.45f;

    [Header("Original Slash Red Edge")]
    [SerializeField, Min(0.5f)] private float edgeWidthPixels = 3f;
    [SerializeField, Min(0.01f)] private float trailDuration = 0.3f;
    [SerializeField, Range(0, 32)] private int trailSparkleCount = 6;
    [SerializeField, Min(0.01f)] private float sparkleSize = 0.12f;

    [Header("Confirmed Hit Flash")]
    [SerializeField, Min(0.01f)] private float impactSize = 0.65f;
    [SerializeField, Min(0.01f)] private float impactDuration = 0.16f;
    [SerializeField, Range(0, 32)] private int impactSparkleCount = 7;
    [SerializeField, Min(0f)] private float impactSparkleSpeed = 2.6f;

    [Header("Player Outline Pulse")]
    [SerializeField] private bool enablePlayerPulse = true;
    [SerializeField, ColorUsage(true, true)] private Color playerPulseColor = new Color(2.2f, 0.1f, 0.2f, 1f);
    [SerializeField, Min(0.5f)] private float playerEdgePixels = 2.5f;
    [SerializeField, Range(0f, 2f)] private float playerPulseIntensity = 0.9f;
    [SerializeField, Min(0.01f)] private float playerPulseDuration = 0.075f;
    [SerializeField, Min(1f)] private float finisherPulseMultiplier = 1.35f;

    [Header("Finisher Pose Afterimage")]
    [SerializeField] private bool enableFinisherAfterimage = true;
    [SerializeField, Min(0.01f)] private float afterimageDuration = 0.18f;
    [SerializeField, Range(0f, 1f)] private float afterimageOpacity = 0.5f;
    [SerializeField, Min(0f)] private float afterimageBackOffset = 0.14f;

    public bool IsConfigured => starlightMaterial != null;
    public TripleSlashData PreparingSkill { get; private set; }
    private enum VisualMode { None, Preparation, Slash, Impact, PlayerPulse }
    private VisualMode mode;
    private Player owner;
    private float direction;
    private float age;
    private float lifetime;
    private bool finisher;
    private bool cancelled;
    private float cancelAge;
    private SpriteRenderer sourceSprite;
    private SpriteRenderer edgeSprite;
    private SpriteRenderer poseAfterimage;
    private bool poseCapturePending;
    private float capturedAlpha;
    private MeshRenderer glint;
    private Mesh quad;
    private ParticleSystem sparkles;
    private MaterialPropertyBlock properties;
    private readonly ParticleSystem.Particle[] particles = new ParticleSystem.Particle[64];

    public void BeginPreparation(Player player, TripleSlashData skill)
    {
        if (!IsConfigured || player == null) { Destroy(gameObject); return; }
        mode = VisualMode.Preparation;
        owner = player;
        PreparingSkill = skill;
        direction = player.isFacingRight ? 1f : -1f;
        lifetime = Mathf.Max(preparationDuration, skill.windupTime + preparationDuration);
        transform.SetParent(player.transform, true);
        transform.position = player.transform.position + new Vector3(preparationOffset.x * direction, preparationOffset.y, 0f);
        glint = CreateGlint();
        sparkles = CreateSparkles(slashColor, lifetime);
        for (int i = 0; i < gatheringCount; i++)
        {
            float angle = i * Mathf.PI * 2f / Mathf.Max(1, gatheringCount);
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * gatheringRadius;
            sparkles.Emit(new ParticleSystem.EmitParams { position = offset, velocity = -offset / Mathf.Max(0.04f, lifetime * 0.8f) }, 1);
        }
        Draw();
    }

    public void PlaySlash(Vector3 position, Quaternion rotation, Vector3 scale, float facing, bool isFinisher, SpriteRenderer originalSprite = null)
    {
        if (!IsConfigured) { Destroy(gameObject); return; }
        mode = VisualMode.Slash;
        finisher = isFinisher;
        direction = facing >= 0f ? 1f : -1f;
        lifetime = trailDuration;
        transform.position = position;
        transform.rotation = rotation;
        transform.localScale = new Vector3(Mathf.Abs(scale.x) * direction, Mathf.Abs(scale.y), 1f);
        sourceSprite = originalSprite;
        if (sourceSprite != null)
        {
            var child = new GameObject("Original Slash Red Edge");
            child.transform.SetParent(sourceSprite.transform, false);
            edgeSprite = child.AddComponent<SpriteRenderer>();
            SetupRenderer(edgeSprite, 2);
            SyncSpriteEdge();
        }
        sparkles = CreateSparkles(finisher ? finisherColor : slashColor, lifetime);
        for (int i = 0; i < trailSparkleCount; i++)
        {
            float t = (i + Random.value * 0.5f) / Mathf.Max(1, trailSparkleCount);
            Vector3 point = new Vector3(Mathf.Lerp(-1.25f, 1.25f, t), Random.Range(-0.12f, 0.12f), 0f);
            sparkles.Emit(new ParticleSystem.EmitParams { position = point, velocity = new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(0.15f, 0.65f), 0f) }, 1);
        }
        Draw();
    }

    public void PlayImpact(Vector3 position, bool isFinisher)
    {
        if (!IsConfigured) { Destroy(gameObject); return; }
        mode = VisualMode.Impact;
        finisher = isFinisher;
        lifetime = impactDuration;
        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));
        glint = CreateGlint();
        sparkles = CreateSparkles(finisher ? finisherColor : slashColor, lifetime);
        for (int i = 0; i < impactSparkleCount; i++)
        {
            float angle = (i + Random.value * 0.5f) * Mathf.PI * 2f / Mathf.Max(1, impactSparkleCount);
            Vector3 velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * impactSparkleSpeed;
            sparkles.Emit(new ParticleSystem.EmitParams { velocity = velocity * Random.Range(0.5f, 1f) }, 1);
        }
        Draw();
    }

    public void PlayPlayerPulse(Player player, bool isFinisher)
    {
        if (!IsConfigured || player == null || player.sr == null ||
            (!enablePlayerPulse && !(isFinisher && enableFinisherAfterimage)))
        {
            Destroy(gameObject);
            return;
        }
        mode = VisualMode.PlayerPulse;
        finisher = isFinisher;
        owner = player;
        sourceSprite = player.sr;
        direction = player.isFacingRight ? 1f : -1f;
        // Keep the captured pose in world space when the player moves or changes facing.
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        transform.localScale = Vector3.one;
        lifetime = Mathf.Max(playerPulseDuration, isFinisher && enableFinisherAfterimage ? afterimageDuration : 0f);
        if (enablePlayerPulse)
        {
            var child = new GameObject("Slash Player Red Outline");
            child.transform.SetParent(sourceSprite.transform, false);
            edgeSprite = child.AddComponent<SpriteRenderer>();
            SetupRenderer(edgeSprite, 2);
            SyncSpriteEdge();
        }
        // Capture after Animator evaluation, so the finisher uses this frame's attack pose.
        poseCapturePending = isFinisher && enableFinisherAfterimage;
        Draw();
    }

    public void CancelPreparation()
    {
        if (mode != VisualMode.Preparation || cancelled) return;
        cancelled = true;
        cancelAge = age;
    }

    private void LateUpdate()
    {
        if (mode == VisualMode.Slash || mode == VisualMode.PlayerPulse) SyncSpriteEdge();
        if (poseCapturePending)
        {
            poseCapturePending = false;
            CaptureFinisherPose();
        }
        if (mode == VisualMode.PlayerPulse) Draw();
    }

    private void CaptureFinisherPose()
    {
        if (sourceSprite == null || sourceSprite.sprite == null || !sourceSprite.enabled || !sourceSprite.gameObject.activeInHierarchy) return;
        var child = new GameObject("Slash Finisher Pose Afterimage");
        child.transform.SetParent(transform, false);
        child.transform.SetPositionAndRotation(sourceSprite.transform.position + Vector3.left * direction * afterimageBackOffset, sourceSprite.transform.rotation);
        Vector3 scale = sourceSprite.transform.lossyScale;
        child.transform.localScale = new Vector3(scale.x * (sourceSprite.flipX ? -1f : 1f), scale.y * (sourceSprite.flipY ? -1f : 1f), scale.z);
        poseAfterimage = child.AddComponent<SpriteRenderer>();
        SetupRenderer(poseAfterimage, 3);
        poseAfterimage.sprite = sourceSprite.sprite;
        poseAfterimage.sortingLayerID = sourceSprite.sortingLayerID;
        poseAfterimage.sortingOrder = sourceSprite.sortingOrder - 1;
        capturedAlpha = sourceSprite.color.a;
        ApplySpriteTexture(poseAfterimage, sourceSprite.sprite, playerEdgePixels);
    }

    private void SyncSpriteEdge()
    {
        if (sourceSprite == null || edgeSprite == null) return;
        edgeSprite.sprite = sourceSprite.sprite;
        edgeSprite.enabled = sourceSprite.enabled;
        // Mirror the geometry explicitly; the edge pass samples the same sprite UVs.
        edgeSprite.transform.localScale = new Vector3(sourceSprite.flipX ? -1f : 1f, sourceSprite.flipY ? -1f : 1f, 1f);
        ApplySpriteTexture(edgeSprite, sourceSprite.sprite, mode == VisualMode.PlayerPulse ? playerEdgePixels : edgeWidthPixels);
        if (mode == VisualMode.PlayerPulse)
        {
            edgeSprite.sortingLayerID = sourceSprite.sortingLayerID;
            edgeSprite.sortingOrder = sourceSprite.sortingOrder + 1;
        }
    }

    private void ApplySpriteTexture(SpriteRenderer renderer, Sprite sprite, float widthPixels)
    {
        renderer.GetPropertyBlock(properties);
        properties.SetFloat("_EdgePixels", widthPixels);
        if (sprite != null)
        {
            Texture2D texture = sprite.texture;
            properties.SetTexture("_MainTex", texture);
            properties.SetVector("_MainTex_TexelSize", new Vector4(1f / texture.width, 1f / texture.height, texture.width, texture.height));
        }
        renderer.SetPropertyBlock(properties);
    }

    private MeshRenderer CreateGlint()
    {
        var child = new GameObject("Sword Starlight");
        child.transform.SetParent(transform, false);
        quad = new Mesh { name = "Slash Glint Quad (Runtime)" };
        quad.vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f) };
        quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
        quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        quad.RecalculateBounds();
        child.AddComponent<MeshFilter>().sharedMesh = quad;
        var renderer = child.AddComponent<MeshRenderer>();
        SetupRenderer(renderer, 1);
        return renderer;
    }

    private ParticleSystem CreateSparkles(Color color, float duration)
    {
        var child = new GameObject("Starlight Motes");
        child.transform.SetParent(transform, false);
        var ps = child.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(sparkleSize * 0.6f, sparkleSize);
        main.startLifetime = duration;
        main.startColor = Color.white;
        main.maxParticles = 64;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = ps.emission; emission.enabled = false;
        var shape = ps.shape; shape.enabled = false;
        var colorLife = ps.colorOverLifetime; colorLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
        colorLife.color = gradient;
        SetupRenderer(ps.GetComponent<ParticleSystemRenderer>(), 1);
        ps.Play();
        return ps;
    }

    private void SetupRenderer(Renderer renderer, int shape)
    {
        renderer.gameObject.layer = gameObject.layer;
        renderer.sharedMaterial = starlightMaterial;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;
        properties ??= new MaterialPropertyBlock();
        properties.Clear();
        properties.SetFloat("_Shape", shape);
        renderer.SetPropertyBlock(properties);
    }

    private void Update()
    {
        if (mode == VisualMode.None) return;
        age += Time.deltaTime;
        if (mode == VisualMode.Preparation)
        {
            if (owner == null) { Destroy(gameObject); return; }
            transform.position = owner.transform.position + new Vector3(preparationOffset.x * direction, preparationOffset.y, 0f);
            int count = sparkles.GetParticles(particles);
            for (int i = 0; i < count; i++)
            {
                var particle = particles[i];
                particle.velocity = -particle.position / Mathf.Max(0.025f, lifetime - age);
                particles[i] = particle;
            }
            sparkles.SetParticles(particles, count);
        }
        if (age >= lifetime || (cancelled && age - cancelAge >= 0.08f)) { Destroy(gameObject); return; }
        Draw();
    }

    private void Draw()
    {
        float t = Mathf.Clamp01(age / Mathf.Max(0.01f, lifetime));
        float fade = Mathf.Pow(1f - t, 1.5f);
        if (cancelled) fade *= Mathf.Clamp01(1f - (age - cancelAge) / 0.08f);
        Color color = finisher ? finisherColor : slashColor;
        if (mode == VisualMode.PlayerPulse)
        {
            float pulse = Mathf.Pow(Mathf.Clamp01(1f - age / playerPulseDuration), 1.5f);
            if (edgeSprite != null)
                Tint(edgeSprite, playerPulseColor, pulse * playerPulseIntensity * (finisher ? finisherPulseMultiplier : 1f) * (sourceSprite != null ? sourceSprite.color.a : 0f));
            if (poseAfterimage != null)
                Tint(poseAfterimage, playerPulseColor, afterimageOpacity * capturedAlpha * Mathf.Pow(Mathf.Clamp01(1f - age / afterimageDuration), 1.5f));
        }
        else if (mode == VisualMode.Slash)
        {
            if (edgeSprite != null) Tint(edgeSprite, color, fade * (sourceSprite != null ? sourceSprite.color.a : 1f));
        }
        else if (glint != null)
        {
            float size = mode == VisualMode.Impact ? impactSize * (finisher ? 1.4f : 1f) : preparationSize;
            glint.transform.localScale = Vector3.one * size * Mathf.Lerp(0.65f, 1.2f, t);
            Tint(glint, color, fade);
        }
        if (sparkles != null) Tint(sparkles.GetComponent<ParticleSystemRenderer>(), color, fade);
    }

    private void Tint(Renderer renderer, Color color, float opacity)
    {
        renderer.GetPropertyBlock(properties);
        properties.SetColor("_Tint", color);
        properties.SetFloat("_Opacity", opacity);
        renderer.SetPropertyBlock(properties);
    }

    private void OnDestroy()
    {
        if (edgeSprite != null) Destroy(edgeSprite.gameObject);
        if (quad != null) Destroy(quad);
    }
}
