using UnityEngine;

/// <summary>
/// 천장에서 떨어지는 돌 (맞으면 체력이 깎인다).
///
///   예고    떨어질 자리 바닥에 붉은 빛이 깜빡이고, 천장에서 돌가루가 조금 떨어진다 (warnTime초)
///   낙하    이끼 낀 돌(BossFallRock)이 돌면서 떨어진다. 닿으면 플레이어 체력 -damage (한 번만)
///   착지    바닥에 부딪혀 파편·먼지·작은 균열, 화면이 살짝 흔들리고 사라진다
///
///   FallingRockHazard.Drop(groundPoint, damage);
/// 실제 시간이 아니라 게임 시간으로 움직인다 (일시정지·히트스탑 때 같이 멈춤).
/// </summary>
public class FallingRockHazard : MonoBehaviour
{
    private const float WarnTime = 0.8f;
    private const float Gravity = 26f;
    private const float Size = 1.3f;          // 돌 크기 (칸)
    private const float HitRadius = 0.6f;

    private Vector3 ground;
    private float topY, damage, t, vy;
    private bool falling, hitDone;
    private SpriteRenderer rock, mark;
    private float spin;

    public static FallingRockHazard Drop(Vector3 groundPoint, float damage, float maxHeight = 10f)
    {
        GameObject go = new GameObject("Falling Rock Hazard");
        FallingRockHazard h = go.AddComponent<FallingRockHazard>();
        h.damage = damage;
        h.Setup(groundPoint, maxHeight);
        return h;
    }

    private void Setup(Vector3 point, float maxHeight)
    {
        int groundMask = LayerMask.GetMask("Ground");
        if (groundMask == 0) groundMask = Physics2D.DefaultRaycastLayers;

        // 실제 바닥 높이
        RaycastHit2D down = Physics2D.Raycast(point + Vector3.up * 2f, Vector2.down, 12f, groundMask);
        ground = down.collider != null ? (Vector3)down.point : point;
        // 천장이 있으면 천장 바로 밑에서, 없으면 화면 위쪽에서
        RaycastHit2D up = Physics2D.Raycast(ground + Vector3.up * 1.5f, Vector2.up, maxHeight, groundMask);
        topY = up.collider != null ? up.point.y - 0.4f : ground.y + maxHeight;
        transform.position = ground;

        // 바닥 예고
        GameObject m = new GameObject("Mark");
        m.transform.SetParent(transform, false);
        mark = m.AddComponent<SpriteRenderer>();
        mark.sprite = BossFxArt.UpFade;
        mark.sharedMaterial = BossFxArt.Unlit;
        mark.sortingLayerName = "Effects";
        mark.sortingOrder = 19;
        Vector2 ms = mark.sprite.bounds.size;
        m.transform.localScale = new Vector3(Size * 1.3f / ms.x, 1.6f / ms.y, 1f);
        m.transform.localPosition = new Vector3(0f, 0.8f, 0f);

        // 돌
        GameObject r = new GameObject("Rock");
        r.transform.SetParent(transform, false);
        rock = r.AddComponent<SpriteRenderer>();
        rock.sprite = BossStageFx.RandomFallRock() ?? GolemBossVisual.Square;
        rock.sharedMaterial = BossFxArt.Unlit;
        rock.sortingLayerName = "Effects";
        rock.sortingOrder = 21;
        rock.color = new Color(0.9f, 0.92f, 0.96f, 1f);
        float k = Size / Mathf.Max(rock.sprite.bounds.size.x, rock.sprite.bounds.size.y);
        r.transform.localScale = new Vector3(Random.value < 0.5f ? -k : k, k, 1f);
        r.transform.position = new Vector3(ground.x, topY, 0f);
        rock.enabled = false;
        spin = Random.Range(-220f, 220f);

        // 천장에서 돌가루가 먼저 조금
        Bounds b = new Bounds(new Vector3(ground.x, topY, 0f), new Vector3(1.5f, 0.1f, 1f));
        BossStageFx.RockFall(b, 2, 0.4f);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        t += dt;

        if (!falling)
        {
            float blink = 0.55f + 0.45f * Mathf.Sin(t * 22f);
            mark.color = new Color(1f, 0.25f, 0.15f, Mathf.Lerp(0.15f, 0.55f, t / WarnTime) * blink);
            if (t >= WarnTime) { falling = true; rock.enabled = true; vy = 0f; }
            return;
        }

        mark.color = new Color(1f, 0.3f, 0.15f, 0.55f);
        Transform rt = rock.transform;
        vy -= Gravity * dt;
        Vector3 p = rt.position + new Vector3(0f, vy * dt, 0f);
        rt.position = p;
        rt.Rotate(0f, 0f, spin * dt);

        if (!hitDone) CheckHit(p);

        float bottom = p.y - Size * 0.45f;
        if (bottom <= ground.y) Land();
    }

    private void CheckHit(Vector3 p)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(p, HitRadius))
        {
            PlayerHealth player = c.GetComponentInParent<PlayerHealth>();
            if (player == null || player.IsDead) continue;
            hitDone = true;
            player.TakeDamage(damage, (Vector2)p);
            CameraShake.Shake(CameraShake.Light);
            return;
        }
    }

    private void Land()
    {
        Vector3 g = new Vector3(ground.x, ground.y, 0f);
        BossStageFx.Debris(g + Vector3.up * 0.3f, 6, 3f, 5f);
        BossFog.LandingDust(g, 2f);
        BossStageFx.Crack(g, g.y, 2.5f, 1.2f);
        CameraShake.Shake(CameraShake.Light);
        Destroy(gameObject);
    }
}
