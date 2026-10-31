using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 각성할 때 발밑에서 파란 여우불이 터지는 연출 (미드저니 그림 예시 적용).
///
///   모으기 (0 ~ 0.25초)   발밑에 작은 불이 모이며 조금씩 커짐
///   방출   (0.25 ~ 0.4초)  확 커지며 가장 밝아짐 (각성 섬광과 같은 박자)
///   사라짐 (0.4 ~ 1.0초)   천천히 흐려지며 사라짐
///
/// 기존 각성 연출(PlayerAwakeningVisual: 몸 발광·오라·화면 테두리)은 그대로 두고 그 위에 얹는다.
/// PlayerAwakening.OnAwakeningStarted만 듣는다. 플레이어 프리팹은 건드리지 않고, 씬이 열릴 때 스스로 붙는다.
/// 그림: Resources/Presentation/Awakening/AwakeningFx_Burst (피벗 = 바닥 고리 가운데). 없으면 아무것도 안 한다.
/// 캐릭터 바로 뒤(Player 레이어, 순서 -5)에 그려서 캐릭터를 가리지 않는다.
/// </summary>
[DisallowMultipleComponent]
public class AwakeningBurstFx : MonoBehaviour
{
    [Tooltip("다 터졌을 때 불꽃 그림의 가로 폭 (월드 칸). 캐릭터 키가 약 2.7")]
    [SerializeField] private float width = 3.4f;
    [Tooltip("발밑 기준 위치 보정")]
    [SerializeField] private Vector2 offset = new Vector2(0f, -0.05f);
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.95f;

    private const string SpritePath = "Presentation/Awakening/AwakeningFx_Burst";

    private Player player;
    private PlayerAwakening awakening;
    private Collider2D body;
    private Sprite sprite;

    private SpriteRenderer burst;
    private float t = -1f;

    // ── 붙이기 ─────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryAttach();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryAttach();

    private static void TryAttach()
    {
        Player p = FindAnyObjectByType<Player>();
        if (p != null && p.GetComponent<AwakeningBurstFx>() == null) p.gameObject.AddComponent<AwakeningBurstFx>();
    }

    // ── 준비 ───────────────────────────────

    private void Start()
    {
        player = GetComponent<Player>();
        awakening = GetComponent<PlayerAwakening>();
        foreach (Collider2D c in GetComponents<Collider2D>())
            if (!c.isTrigger) { body = c; break; }   // 몸 충돌 범위 (감지용 트리거 말고)
        sprite = Resources.Load<Sprite>(SpritePath);
        if (awakening == null || sprite == null) { enabled = false; return; }
        awakening.OnAwakeningStarted += Play;
    }

    private void OnDestroy()
    {
        if (awakening != null) awakening.OnAwakeningStarted -= Play;
        if (burst != null) Destroy(burst.gameObject);
    }

    private void Play()
    {
        if (burst == null)
        {
            GameObject go = new GameObject("AwakeningBurst");
            burst = go.AddComponent<SpriteRenderer>();
            burst.sprite = sprite;
            burst.sharedMaterial = BossFxArt.Unlit;
            burst.sortingLayerName = "Player";
            burst.sortingOrder = -5;
        }
        // 불꽃이 오른쪽으로 기울어 있어서, 왼쪽을 보고 있으면 뒤집는다
        bool facingLeft = transform.lossyScale.x < 0f || (player != null && player.sr != null && player.sr.flipX);
        burst.flipX = facingLeft;
        t = 0f;
        burst.enabled = true;
        Apply();
    }

    private void LateUpdate()
    {
        if (t < 0f || burst == null) return;
        t += Time.unscaledDeltaTime;   // 각성 도입 중 게임이 멈춰도 연출은 흐른다
        if (t >= 1f)
        {
            t = -1f;
            burst.enabled = false;
            return;
        }
        Apply();
    }

    private void Apply()
    {
        float scale, alpha;
        if (t < 0.25f)            // 모으기
        {
            float k = t / 0.25f;
            scale = Mathf.Lerp(0.3f, 0.55f, k);
            alpha = Mathf.Lerp(0f, 0.55f, k);
        }
        else if (t < 0.4f)        // 방출
        {
            float k = (t - 0.25f) / 0.15f;
            k = 1f - (1f - k) * (1f - k);
            scale = Mathf.Lerp(0.55f, 1.15f, k);
            alpha = Mathf.Lerp(0.55f, 1f, k);
        }
        else                      // 사라짐
        {
            float k = (t - 0.4f) / 0.6f;
            scale = Mathf.Lerp(1.15f, 1.05f, k);
            alpha = 1f - k * k;
        }

        float s = width * scale / sprite.bounds.size.x;
        float flicker = 1f + 0.04f * Mathf.Sin(t * 40f);
        burst.transform.localScale = new Vector3(s, s * flicker, 1f);
        burst.transform.position = Feet() + (Vector3)offset;
        Color c = Color.white;
        c.a = alpha * maxAlpha;
        burst.color = c;
    }

    // 발밑: 충돌 범위 아래쪽 가운데
    private Vector3 Feet()
    {
        if (body != null)
        {
            Bounds b = body.bounds;
            return new Vector3(b.center.x, b.min.y, 0f);
        }
        return transform.position;
    }
}
