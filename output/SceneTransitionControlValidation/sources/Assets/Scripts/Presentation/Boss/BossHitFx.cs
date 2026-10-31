using UnityEngine;

/// <summary>
/// 보스 피격 반응: 맞은 쪽에서 작은 돌가루·파편이 튄다. 몸체 번쩍임은 기본으로 끈다.
/// EnemyHealth 체력이 줄어든 걸 보고 알아챈다 (적 코드는 고치지 않음). BossRoom이 보스와 갈라진 조각에 붙인다.
/// 번쩍임 셰이더: Resources/Presentation/Boss/SpriteSolidFlash
/// </summary>
[DefaultExecutionOrder(1000)]   // 골렘 그림이 바뀐 다음에 따라 그린다
public class BossHitFx : MonoBehaviour
{
    [SerializeField] private bool flashOnHit = false;
    private const float FlashTime = 0.09f;
    private static Material flashMat;

    private EnemyHealth health;
    private SpriteRenderer body, flash;
    private float lastHp, flashT = -1f;
    private Transform player;

    private void Start()
    {
        health = GetComponent<EnemyHealth>();
        body = GetComponent<SpriteRenderer>();
        if (health == null) { enabled = false; return; }
        lastHp = health.CurrentHp;

        if (flashOnHit && flashMat == null)
        {
            Shader sh = Resources.Load<Shader>("Presentation/Boss/SpriteSolidFlash");
            if (sh != null && sh.isSupported) flashMat = new Material(sh) { hideFlags = HideFlags.DontSave };
        }
        if (flashOnHit && body != null && flashMat != null)
        {
            GameObject go = new GameObject("HitFlash");
            go.transform.SetParent(transform, false);
            flash = go.AddComponent<SpriteRenderer>();
            flash.sharedMaterial = flashMat;
            flash.enabled = false;
        }
        Player p = FindAnyObjectByType<Player>();
        if (p != null) player = p.transform;
    }

    private void LateUpdate()
    {
        float hp = health.CurrentHp;
        if (hp < lastHp - 0.01f && !health.IsDead) OnHit();
        lastHp = hp;

        if (!flashOnHit) { if (flash != null) flash.enabled = false; return; }
        if (flash == null) return;
        if (flashT >= 0f)
        {
            flashT += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(flashT / FlashTime);
            bool show = k > 0f && body != null && body.enabled && body.sprite != null;
            flash.enabled = show;
            if (show)
            {
                flash.sprite = body.sprite;
                flash.flipX = body.flipX;
                flash.flipY = body.flipY;
                flash.drawMode = body.drawMode;
                flash.size = body.size;
                flash.sortingLayerID = body.sortingLayerID;
                flash.sortingOrder = body.sortingOrder + 5;
                flash.color = new Color(1f, 1f, 1f, 0.85f * k);
            }
            else if (k <= 0f) flashT = -1f;
        }
    }

    private void OnHit()
    {
        flashT = 0f;
        Bounds b = body != null ? body.bounds : new Bounds(transform.position, Vector3.one * 2f);
        float side = player != null && player.position.x < b.center.x ? -1f : 1f;   // 맞은 쪽 (플레이어 쪽)
        Vector3 at = new Vector3(b.center.x + side * b.extents.x * 0.6f, b.center.y + Random.Range(-0.3f, 0.6f), 0f);
        BossStageFx.Debris(at, 3, 2.5f, 4f);
        BossFog.HitDust(at, side);
    }

    private void OnDestroy()
    {
        if (flash != null) Destroy(flash.gameObject);
    }
}
