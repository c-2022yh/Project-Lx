using System.Collections.Generic;
using UnityEngine;

// One frozen world-space rectangle is shared by the warning and damage query.
// Committed attacks finish independently; only uncommitted warnings are cancelled.
public class GolemBossArea : MonoBehaviour
{
    public Vector2 Center { get; private set; }
    public Vector2 Size { get; private set; }
    public bool IsCommitted { get; private set; }
    private SpriteRenderer fill;
    private readonly List<SpriteRenderer> borders = new List<SpriteRenderer>();
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly HashSet<PlayerHealth> victims = new HashSet<PlayerHealth>();
    private Color warningColor;
    private DamageInfo damage;
    private LayerMask targets;
    private float elapsed, lifetime;
    private GameObject impactPrefab;
    private SpriteRenderer fallingRock;
    private float dropHeight;
    // Presentation only (BossFx): default art rises from the ground; thin areas are summon markers.
    private bool isMarker, growFromGround;
    private float shownProgress;
    private Vector2 GroundCenter => Center - Vector2.up * Size.y * 0.5f;

    public void SetFallingRock(Sprite sprite, float height)
    {
        fallingRock = GolemBossVisual.MakeRectangle("Falling Rock", transform, Color.gray, 22);
        if (sprite != null) { fallingRock.sprite = sprite; fallingRock.color = Color.white; }
        Vector2 size = fallingRock.sprite.bounds.size;
        fallingRock.transform.localScale = new Vector3(1f / Mathf.Max(0.01f, size.x), 1f / Mathf.Max(0.01f, size.y), 1f);
        dropHeight = Mathf.Max(1f, height);
        fallingRock.enabled = false;
    }

    public static GolemBossArea Create(Vector2 center, Vector2 size, Color color, Sprite fillSprite, Sprite borderSprite)
    {
        var obj = new GameObject("Golem Warning");
        obj.transform.position = center;
        var area = obj.AddComponent<GolemBossArea>();
        area.Center = center;
        area.Size = new Vector2(Mathf.Max(0.05f, size.x), Mathf.Max(0.05f, size.y));
        area.warningColor = color;
        area.fill = GolemBossVisual.MakeRectangle("Area", obj.transform, color, 20);
        area.fill.sharedMaterial = BossFxArt.Unlit;
        area.isMarker = area.Size.y <= 0.3f;
        if (area.isMarker && fillSprite == null)
        {
            // Summon marker: a soft glowing ellipse on the ground instead of a box.
            area.SetRectangle(area.fill, Vector2.zero, new Vector2(area.Size.x * 1.4f, Mathf.Max(0.45f, area.Size.x * 0.3f)), BossFxArt.SoftDot);
            area.SetProgress(0f);
            return area;
        }
        area.growFromGround = fillSprite == null;
        area.SetRectangle(area.fill, Vector2.zero, area.Size, fillSprite != null ? fillSprite : BossFxArt.UpFade);
        float edge = Mathf.Min(0.08f, Mathf.Min(area.Size.x, area.Size.y) * 0.15f);
        // Default look: open top, bright ground line and sides (moon style). Custom border art keeps the full box.
        if (borderSprite != null)
            area.AddEdge(new Vector2(0f, (area.Size.y - edge) / 2f), new Vector2(area.Size.x, edge), borderSprite);
        float floorEdge = borderSprite != null ? edge : edge * 1.6f;
        area.AddEdge(new Vector2(0f, -(area.Size.y - floorEdge) / 2f), new Vector2(area.Size.x, floorEdge), borderSprite);
        area.AddEdge(new Vector2((area.Size.x - edge) / 2f, 0f), new Vector2(edge, area.Size.y), borderSprite);
        area.AddEdge(new Vector2(-(area.Size.x - edge) / 2f, 0f), new Vector2(edge, area.Size.y), borderSprite);
        area.SetProgress(0f);
        return area;
    }

    private void SetRectangle(SpriteRenderer renderer, Vector2 offset, Vector2 size, Sprite sprite)
    {
        if (sprite != null) renderer.sprite = sprite;
        renderer.transform.localPosition = offset;
        Vector2 bounds = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(size.x / Mathf.Max(0.001f, bounds.x), size.y / Mathf.Max(0.001f, bounds.y), 1f);
        // Custom sprites must use centered pivots; geometry stays world-axis aligned.
    }

    private void AddEdge(Vector2 offset, Vector2 size, Sprite sprite)
    {
        var border = GolemBossVisual.MakeRectangle("Border", transform, warningColor, 21);
        border.sharedMaterial = BossFxArt.Unlit;
        SetRectangle(border, offset, size, sprite);
        borders.Add(border);
    }

    public void SetProgress(float progress)
    {
        if (IsCommitted || fill == null) return;
        float p = Mathf.Clamp01(progress);
        shownProgress = p;
        Color color = warningColor;
        if (isMarker && borders.Count == 0)
        {
            color.a = Mathf.Lerp(0.25f, 0.8f, p) * (0.85f + 0.15f * Mathf.Sin(Time.time * 10f));
            fill.color = color;
            if (Random.value < Time.deltaTime * 12f)
                BossFx.MarkerSpark(GroundCenter, Size.x, new Color(warningColor.r, warningColor.g, warningColor.b, 0.9f));
            return;
        }
        color.a = Mathf.Lerp(0.18f, 0.55f, p);
        fill.color = color;
        if (growFromGround)
        {
            // The warning fills up from the ground as the hit gets closer.
            float h = Size.y * Mathf.Lerp(0.15f, 1f, p);
            SetRectangle(fill, new Vector2(0f, (h - Size.y) * 0.5f), new Vector2(Size.x, h), null);
        }
        color.a = 0.9f;
        foreach (var border in borders) border.color = color;
        if (fallingRock != null)
        {
            fallingRock.enabled = progress >= 0.65f;
            float height = Mathf.Lerp(dropHeight, 0.5f, Mathf.Clamp01((progress - 0.65f) / 0.35f));
            fallingRock.transform.localPosition = Vector2.up * (height - Size.y * 0.5f);
        }
    }

    public void Commit(DamageInfo info, LayerMask targetLayers, float activeDuration, GameObject impact)
    {
        if (IsCommitted) return;
        IsCommitted = true;
        if (fallingRock != null) fallingRock.enabled = false;
        damage = info;
        targets = targetLayers;
        lifetime = Mathf.Max(0.02f, activeDuration);
        impactPrefab = impact;
        fill.color = new Color(1f, 0.65f, 0.15f, 0.65f);
        if (growFromGround) SetRectangle(fill, Vector2.zero, Size, null);
        foreach (var border in borders) border.enabled = false;
        if (impactPrefab != null)
        {
            GameObject effect = Instantiate(impactPrefab, Center, Quaternion.identity);
            Destroy(effect, 5f);
        }
        else if (!isMarker)
        {
            // No impact art yet: code-drawn ring, dust, debris and shake.
            BossFx.Impact(Center, Size);
        }
        name = "Golem Impact";
        DealDamage();
    }

    private void FixedUpdate()
    {
        if (!IsCommitted) return;
        if (damage.attacker == null) { Destroy(gameObject); return; }
        DealDamage();
        elapsed += Time.fixedDeltaTime;
        if (elapsed >= lifetime) Destroy(gameObject);
    }

    private void DealDamage()
    {
        var filter = new ContactFilter2D();
        filter.SetLayerMask(targets);
        filter.useTriggers = true;
        Physics2D.OverlapBox(Center, Size, 0f, filter, overlaps);
        foreach (Collider2D other in overlaps)
        {
            PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
            if (player == null || !player.isActiveAndEnabled || player.IsDead || victims.Contains(player)) continue;
            PlayerHitReaction reaction = player.GetComponent<PlayerHitReaction>();
            if (reaction != null && !reaction.CanTakeDamage()) continue;
            victims.Add(player);
            player.TakeDamage(damage, ((Vector2)player.transform.position - Center).normalized);
        }
    }

    public void CancelWarning()
    {
        if (IsCommitted) return;
        // A summon marker that ran its full time is where a minion just appeared.
        if (isMarker && shownProgress >= 0.95f) BossFx.SummonPop(GroundCenter, Size.x);
        Destroy(gameObject);
    }
}
