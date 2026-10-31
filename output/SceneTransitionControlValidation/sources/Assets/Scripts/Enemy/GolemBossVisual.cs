using System;
using UnityEngine;

[Serializable]
public class GolemBossMotion
{
    public Sprite[] prepare;
    public Sprite[] strike;
    public Sprite[] recovery;
}

public enum GolemBossPose { Idle, Prepare, Strike, Recovery, Summon, PhaseChange }

// Presentation only: replacing sprites never changes attack geometry or timing.
[RequireComponent(typeof(SpriteRenderer), typeof(EnemyHealth))]
public class GolemBossVisual : MonoBehaviour
{
    [Header("Art (empty slots use the block golem)")]
    [SerializeField] private Sprite[] idleSprites;
    [SerializeField] private Sprite[] summonSprites;
    [SerializeField] private Sprite[] phaseChangeSprites;
    [SerializeField, Min(0.01f)] private float idleFrameTime = 0.2f;
    [SerializeField] private bool spritesFaceRight = true;
    [Header("Separate arm art (optional)")]
    [SerializeField] private Sprite leftArmSprite;
    [SerializeField] private Sprite rightArmSprite;
    [SerializeField] private Vector2 leftArmPosition = new Vector2(-1.6f, -0.1f);
    [SerializeField] private Vector2 rightArmPosition = new Vector2(1.6f, -0.1f);
    [SerializeField, Min(0.01f)] private float armScale = 1f;
    [SerializeField, Min(0f)] private float armPrepareLift = 0.65f;
    [SerializeField, Min(0f)] private float armStrikeDrop = 0.25f;
    [Header("Health bar")]
    [SerializeField] private bool showHealthBar = true;
    [SerializeField] private Vector2 healthBarOffset = new Vector2(0f, 2.6f);
    [SerializeField] private Vector2 healthBarSize = new Vector2(4f, 0.16f);

    private SpriteRenderer body, leftArm, rightArm, core, barBack, barFill;
    private EnemyHealth health;
    private GolemBossPose pose;
    private Sprite[] frames;
    private float progress;
    private int facing = 1;
    private bool singleArm;
    private bool presentationVisible = true;
    private static Sprite square;

    // A code-generated white primitive, shared by indicators and placeholder parts.
    public static Sprite Square
    {
        get
        {
            if (square == null)
                square = Sprite.Create(Texture2D.whiteTexture,
                    new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                    new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width, 0, SpriteMeshType.FullRect);
            return square;
        }
    }

    public static SpriteRenderer MakeRectangle(string name, Transform parent, Color color, int order)
    {
        var part = new GameObject(name);
        int effectLayer = LayerMask.NameToLayer("Effects");
        part.layer = effectLayer >= 0 ? effectLayer : 0;
        part.transform.SetParent(parent, false);
        var renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = Square;
        renderer.color = color;
        renderer.sortingLayerName = "Effects";
        renderer.sortingOrder = order;
        return renderer;
    }

    private void Awake()
    {
        // Runtime clones inherit the already-generated parts. Rebuild only these owned parts once.
        foreach (string partName in new[] { "Placeholder Left Arm", "Placeholder Right Arm", "Placeholder Core", "Boss HP Background", "Boss HP" })
        {
            Transform previous = transform.Find(partName);
            if (previous == null) continue;
            previous.gameObject.SetActive(false);
            Destroy(previous.gameObject);
        }
        body = GetComponent<SpriteRenderer>();
        health = GetComponent<EnemyHealth>();
        leftArm = MakeArm("Golem Left Arm", leftArmSprite);
        rightArm = MakeArm("Golem Right Arm", rightArmSprite);
        core = MakeRectangle("Placeholder Core", transform, new Color(1f, 0.65f, 0.15f), 2);
        barBack = MakeRectangle("Boss HP Background", transform, new Color(0.08f, 0.08f, 0.1f), 10);
        barFill = MakeRectangle("Boss HP", transform, new Color(0.85f, 0.12f, 0.08f), 11);
    }

    private SpriteRenderer MakeArm(string partName, Sprite sprite)
    {
        Transform existing = transform.Find(partName);
        SpriteRenderer renderer = existing != null ? existing.GetComponent<SpriteRenderer>() : null;
        if (renderer == null) renderer = MakeRectangle(partName, transform, Color.white, 1);
        renderer.sprite = sprite != null ? sprite : Square;
        renderer.color = sprite != null ? Color.white : new Color(0.38f, 0.42f, 0.46f);
        return renderer;
    }

    private void OnEnable() => SetPose(GolemBossPose.Idle);

    public void SetPresentationVisible(bool visible)
    {
        presentationVisible = visible;
        // Another component's OnEnable can run before this component's Awake.
        if (body == null) body = GetComponent<SpriteRenderer>();
        body.enabled = visible;
        foreach (var part in new[] { leftArm, rightArm, core, barBack, barFill })
            if (part != null) part.gameObject.SetActive(visible);
    }

    public void SetPose(GolemBossPose next, float normalizedTime = 0f, int direction = 1, Sprite[] sprites = null, bool oneArm = false)
    {
        pose = next;
        progress = Mathf.Clamp01(normalizedTime);
        facing = direction < 0 ? -1 : 1;
        frames = sprites;
        singleArm = oneArm;
    }

    private void LateUpdate()
    {
        if (!presentationVisible) { body.enabled = false; return; }
        if (health.IsDead)
        {
            leftArm.enabled = rightArm.enabled = core.enabled = false;
            barBack.enabled = barFill.enabled = false;
            if (body.sprite != Square) { body.drawMode = SpriteDrawMode.Simple; body.color = Color.white; }
            return; // EnemyAnimation owns death sprites.
        }
        Sprite[] selected = frames;
        if (selected == null || selected.Length == 0)
        {
            if (pose == GolemBossPose.Idle) selected = idleSprites;
            else if (pose == GolemBossPose.Summon) selected = summonSprites;
            else if (pose == GolemBossPose.PhaseChange) selected = phaseChangeSprites;
            // Static body art also remains visible during attacks without dedicated frames.
            if (selected == null || selected.Length == 0) selected = idleSprites;
        }
        bool hasArt = selected != null && selected.Length > 0;
        if (hasArt)
        {
            int index = pose == GolemBossPose.Idle
                ? (int)(Time.time / Mathf.Max(0.01f, idleFrameTime)) % selected.Length
                : Mathf.Min(selected.Length - 1, (int)(progress * selected.Length));
            body.sprite = selected[index] != null ? selected[index] : Square;
            body.drawMode = SpriteDrawMode.Simple;
            body.color = Color.white;
            body.flipX = leftArmSprite == null && rightArmSprite == null && (spritesFaceRight ? facing < 0 : facing > 0);
        }
        else
        {
            body.sprite = Square;
            body.drawMode = SpriteDrawMode.Sliced;
            body.size = new Vector2(2.6f, 3.6f);
            body.color = new Color(0.48f, 0.53f, 0.58f);
            // Root and collider are centered at (0, 0); feet are at y = -1.8.
            body.flipX = false;
        }
        leftArm.enabled = rightArm.enabled = core.enabled = !hasArt && body.enabled;
        float lift = pose == GolemBossPose.Prepare ? Mathf.Lerp(0.2f, 2f, progress) : 0.2f;
        if (pose == GolemBossPose.Strike) lift = -1.2f;
        bool channel = pose == GolemBossPose.Summon || pose == GolemBossPose.PhaseChange;
        if (channel) lift = 1.4f;
        leftArm.transform.localPosition = new Vector3(-1.7f, singleArm && facing > 0 ? 0.2f : lift);
        rightArm.transform.localPosition = new Vector3(1.7f, singleArm && facing < 0 ? 0.2f : lift);
        leftArm.transform.localScale = rightArm.transform.localScale = new Vector3(1.1f, 1.3f, 1f);
        core.transform.localPosition = new Vector3(0f, 0.3f);
        core.transform.localScale = Vector3.one * (channel ? 0.75f + Mathf.Sin(Time.time * 12f) * 0.1f : 0.55f);
        if (hasArt && (leftArmSprite != null || rightArmSprite != null))
        {
            leftArm.enabled = leftArmSprite != null && body.enabled;
            rightArm.enabled = rightArmSprite != null && body.enabled;
            float armLift = pose == GolemBossPose.Prepare ? Mathf.Lerp(0f, armPrepareLift, progress) : 0f;
            if (pose == GolemBossPose.Strike) armLift = -armStrikeDrop;
            if (pose == GolemBossPose.Recovery) armLift = Mathf.Lerp(-armStrikeDrop, 0f, progress);
            if (channel) armLift = armPrepareLift;
            leftArm.transform.localPosition = leftArmPosition + Vector2.up * (singleArm && facing > 0 ? 0f : armLift);
            rightArm.transform.localPosition = rightArmPosition + Vector2.up * (singleArm && facing < 0 ? 0f : armLift);
            leftArm.transform.localScale = rightArm.transform.localScale = Vector3.one * armScale;
        }
        barBack.enabled = barFill.enabled = showHealthBar;
        float ratio = health.MaxHp > 0f ? Mathf.Clamp01(health.CurrentHp / health.MaxHp) : 0f;
        barBack.transform.localPosition = healthBarOffset;
        barBack.transform.localScale = new Vector3(healthBarSize.x, healthBarSize.y, 1f);
        barFill.transform.localPosition = healthBarOffset + Vector2.left * healthBarSize.x * (1f - ratio) * 0.5f;
        barFill.transform.localScale = new Vector3(healthBarSize.x * ratio, healthBarSize.y * 0.7f, 1f);
    }
}
