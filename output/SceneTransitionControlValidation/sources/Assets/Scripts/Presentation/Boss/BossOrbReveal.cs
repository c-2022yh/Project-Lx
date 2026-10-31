using System.Collections;
using UnityEngine;

/// <summary>
/// 보스를 쓰러뜨린 뒤, 돌 속에 갇혀 있던 보주가 처음 모습을 드러내는 연출.
/// 싸우는 동안에는 보주가 보이지 않는다.
///
///   BossOrbReveal orb = BossOrbReveal.Play(bossCenter, player, sprite);   // 빛나며 나타나 둥실 떠 있음
///   orb.FlyToPlayer();                                                   // 플레이어에게 날아가 스며듦
///   while (!orb.IsDone) yield return null;
///
/// 그림 칸(sprite)을 비우면 코드로 그린 빛 구슬을 쓴다. 실제 시간으로 흐른다.
/// 보이기만 하는 연출이라 보주 개수·에너지는 바꾸지 않는다.
/// </summary>
public class BossOrbReveal : MonoBehaviour
{
    private const string Layer = "Orb";
    private static readonly Color Halo = new Color(0.55f, 0.75f, 1f, 1f);

    private Transform player;
    private SpriteRenderer halo, core, art;
    private bool fly;
    private float bob;

    public bool IsDone { get; private set; }

    public static BossOrbReveal Play(Vector3 at, Transform player, Sprite sprite = null)
    {
        GameObject go = new GameObject("Boss Orb Reveal");
        go.transform.position = at;
        BossOrbReveal orb = go.AddComponent<BossOrbReveal>();
        orb.player = player;

        orb.halo = Part(go.transform, "Halo", BossFxArt.SoftDot, 0, 3.2f);
        orb.core = Part(go.transform, "Core", BossFxArt.SoftDot, 1, 0.9f);
        if (sprite != null)
        {
            orb.art = Part(go.transform, "Art", sprite, 2, 1f);
            Vector2 size = sprite.bounds.size;
            float k = 0.9f / Mathf.Max(0.01f, Mathf.Max(size.x, size.y));
            orb.art.transform.localScale = new Vector3(k, k, 1f);
        }

        go.transform.localScale = Vector3.zero;
        orb.StartCoroutine(orb.Run());
        return orb;
    }

    private static SpriteRenderer Part(Transform parent, string name, Sprite sprite, int order, float scale)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = BossFxArt.Unlit;
        sr.sortingLayerName = Layer;
        sr.sortingOrder = 20 + order;
        return sr;
    }

    /// <summary>떠 있던 보주를 플레이어에게 보낸다.</summary>
    public void FlyToPlayer() => fly = true;

    private IEnumerator Run()
    {
        // 1. 빛나며 나타남
        Ring(transform.position, 0.6f, 5f, 0.6f);
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.5f);
            float s = k < 0.7f ? Mathf.Lerp(0f, 1.2f, k / 0.7f) : Mathf.Lerp(1.2f, 1f, (k - 0.7f) / 0.3f);
            transform.localScale = new Vector3(s, s, 1f);
            Glow();
            yield return null;
        }
        transform.localScale = Vector3.one;

        // 2. 둥실 떠서 기다림
        Vector3 rest = transform.position;
        while (!fly)
        {
            bob += Time.unscaledDeltaTime;
            transform.position = rest + Vector3.up * Mathf.Sin(bob * 2.2f) * 0.12f;
            Glow();
            yield return null;
        }

        // 3. 플레이어에게 날아감 (살짝 위로 휘며)
        if (player != null)
        {
            Vector3 from = transform.position;
            t = 0f;
            const float flySeconds = 1.1f;
            while (t < flySeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / flySeconds);
                float e = k * k * (3f - 2f * k);
                Vector3 to = player.position + Vector3.up * 1.1f;
                Vector3 p = Vector3.Lerp(from, to, e) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.4f;
                transform.position = p;
                float s = Mathf.Lerp(1f, 0.55f, e);
                transform.localScale = new Vector3(s, s, 1f);
                Glow();
                yield return null;
            }

            // 4. 스며듦
            Ring(player.position + Vector3.up * 1.1f, 0.4f, 3.5f, 0.5f);
            t = 0f;
            while (t < 0.45f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.45f);
                if (player != null) transform.position = player.position + Vector3.up * 1.1f;
                transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.6f, k);
                SetAlpha(1f - k);
                yield return null;
            }
        }

        IsDone = true;
        Destroy(gameObject);
    }

    private void Glow()
    {
        float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 4f);
        halo.color = new Color(Halo.r, Halo.g, Halo.b, 0.55f * pulse);
        core.color = new Color(1f, 1f, 1f, 0.95f);
        if (art != null) art.color = Color.white;
    }

    private void SetAlpha(float a)
    {
        halo.color = new Color(Halo.r, Halo.g, Halo.b, 0.55f * a);
        core.color = new Color(1f, 1f, 1f, 0.95f * a);
        if (art != null) art.color = new Color(1f, 1f, 1f, a);
    }

    private static void Ring(Vector3 at, float from, float to, float life)
    {
        BossFxSprite r = BossFxSprite.Create("Boss Orb Ring", BossFxArt.Ring, at, Layer, 19);
        r.unscaled = true;
        r.color = new Color(0.8f, 0.9f, 1f, 0.9f);
        r.scaleFrom = new Vector3(from, from, 1f);
        r.scaleTo = new Vector3(to, to, 1f);
        r.life = life;
    }
}
