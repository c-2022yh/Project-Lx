using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 각성 연출: 발밑에서 파란 여우불이 모였다가 터지고, 각성이 이어지는 동안 계속 타오른다 (미드저니 그림 예시 적용).
///
///   모으기   Q를 누른 순간부터 각성 도입 시간(기본 0.8초) 동안 작은 불이 모이며 커짐
///   방출     도입이 끝나는 순간 확 커지며 가장 밝아짐
///   가라앉기 0.45초 동안 조금 작고 옅어짐
///   유지     각성 중에는 Hold Alpha만큼 남음 (기본 0: 터진 뒤 사라지고 실루엣 아우라가 이어받음)
///   꺼짐     각성이 끝나면 0.3초 동안 사라짐
///
/// 기존 각성 연출(PlayerAwakeningVisual: 몸 발광·오라·화면 테두리)은 그대로 두고 그 위에 얹는다.
/// PlayerAwakening의 IsAwakening / IsAwakened만 읽는다. 플레이어 프리팹은 건드리지 않고, 씬이 열릴 때 스스로 붙는다.
/// 그림: Resources/Presentation/Awakening/AwakeningFx_Burst (피벗 = 바닥 고리 가운데). 없으면 아무것도 안 한다.
/// 캐릭터 그림 바로 뒤에 그려서 캐릭터를 가리지 않는다.
/// </summary>
[DisallowMultipleComponent]
public class AwakeningBurstFx : MonoBehaviour
{
    [Tooltip("가장 크게 터졌을 때 불꽃 그림의 가로 폭 (월드 칸). 캐릭터 키가 약 2.7")]
    [SerializeField] private float width = 2.2f;
    [Tooltip("발밑 기준 위치 보정")]
    [SerializeField] private Vector2 offset = new Vector2(0f, -0.05f);
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.95f;

    [Header("각성 유지 중")]
    [Tooltip("유지 중 크기 (터질 때 = 1.15)")]
    [SerializeField, Range(0.3f, 1.2f)] private float holdScale = 0.85f;
    [SerializeField, Range(0f, 1f)] private float holdAlpha = 0f;   // 각성 유지 중 아우라는 AwakeningSilhouetteAura가 맡는다 (0 = 터진 뒤 사라짐)

    private const string SpritePath = "Presentation/Awakening/AwakeningFx_Burst";
    private const float Release = 0.15f, Settle = 0.45f, FadeOut = 0.3f;

    private Player player;
    private PlayerAwakening awakening;
    private Collider2D body;
    private Sprite sprite;
    private SpriteRenderer burst;

    private enum Phase { Off, Intro, On, Ending }
    private Phase phase = Phase.Off;
    private float t;          // 지금 단계가 시작된 뒤 흐른 시간
    private float introLen;   // 모으기 시간
    private float endFrom;    // 꺼지기 시작할 때의 밝기
    private float endScale;

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
        if (awakening == null || sprite == null)
        {
            Debug.LogWarning($"[각성 연출] 꺼짐: PlayerAwakening {(awakening != null ? "있음" : "없음")}, 그림 {(sprite != null ? "있음" : "없음")}");
            enabled = false;
            return;
        }
        Debug.Log("[각성 연출] 플레이어에 붙었습니다. Q로 각성하면 발밑 여우불이 나옵니다.");
    }

    private void OnDestroy()
    {
        if (burst != null) Destroy(burst.gameObject);
    }

    private void EnsureRenderer()
    {
        if (burst != null) return;
        GameObject go = new GameObject("AwakeningBurst");
        burst = go.AddComponent<SpriteRenderer>();
        burst.sprite = sprite;
        burst.sharedMaterial = BossFxArt.Unlit;
        burst.enabled = false;
    }

    // ── 매 프레임 ──────────────────────────

    private void LateUpdate()
    {
        bool intro = awakening.IsAwakening;
        bool on = awakening.IsAwakened;
        float dt = Time.unscaledDeltaTime;   // 각성 도입 중 게임이 멈춰도 연출은 흐른다

        switch (phase)
        {
            case Phase.Off:
                if (intro || on)
                {
                    EnsureRenderer();
                    introLen = intro ? Mathf.Max(0.1f, awakening.IntroDuration - awakening.IntroElapsed) : 0.1f;
                    phase = Phase.Intro;
                    t = 0f;
                    burst.enabled = true;
                }
                break;
            case Phase.Intro:
                t += dt;
                if (!intro && on) { phase = Phase.On; t = 0f; }
                else if (!intro && !on) BeginEnding();
                break;
            case Phase.On:
                t += dt;
                if (!on) BeginEnding();
                break;
            case Phase.Ending:
                t += dt;
                if (intro || on) { phase = Phase.Off; break; }   // 끝나자마자 다시 각성하면 처음부터
                if (t >= FadeOut)
                {
                    phase = Phase.Off;
                    burst.enabled = false;
                    return;
                }
                break;
        }

        if (phase == Phase.Off || burst == null) return;
        Evaluate(out float scale, out float alpha);
        Apply(scale, alpha);
    }

    private void BeginEnding()
    {
        Evaluate(out endScale, out endFrom);
        phase = Phase.Ending;
        t = 0f;
    }

    private void Evaluate(out float scale, out float alpha)
    {
        float time = Time.unscaledTime;
        switch (phase)
        {
            case Phase.Intro:   // 모으기
            {
                float k = Mathf.Clamp01(t / introLen);
                scale = Mathf.Lerp(0.3f, 0.6f, k);
                alpha = Mathf.Lerp(0f, 0.6f, k);
                return;
            }
            case Phase.On:
            {
                if (t < Release)   // 방출
                {
                    float k = t / Release;
                    k = 1f - (1f - k) * (1f - k);
                    scale = Mathf.Lerp(0.6f, 1.15f, k);
                    alpha = Mathf.Lerp(0.6f, 1f, k);
                    return;
                }
                float wobbleS = 1f + 0.05f * Mathf.Sin(time * 5.3f) + 0.02f * Mathf.Sin(time * 11.7f);
                float wobbleA = 1f + 0.12f * Mathf.Sin(time * 3.1f);
                float s = Mathf.Clamp01((t - Release) / Settle);   // 가라앉기
                s = s * s * (3f - 2f * s);
                scale = Mathf.Lerp(1.15f, holdScale * wobbleS, s);
                alpha = Mathf.Lerp(1f, holdAlpha * wobbleA, s);
                return;
            }
            case Phase.Ending:
            {
                float k = Mathf.Clamp01(t / FadeOut);
                scale = Mathf.Lerp(endScale, endScale * 0.9f, k);
                alpha = endFrom * (1f - k) * (1f - k);
                return;
            }
        }
        scale = 0f;
        alpha = 0f;
    }

    private void Apply(float scale, float alpha)
    {
        // 캐릭터 그림 바로 뒤 (기존 오라보다도 뒤)
        SpriteRenderer src = player != null ? player.sr : null;
        if (src != null)
        {
            burst.sortingLayerID = src.sortingLayerID;
            burst.sortingOrder = src.sortingOrder - 2;
        }
        else
        {
            burst.sortingLayerName = "Player";
            burst.sortingOrder = -5;
        }

        // 불꽃이 오른쪽으로 기울어 있어서, 왼쪽을 보고 있으면 뒤집는다
        burst.flipX = transform.lossyScale.x < 0f || (src != null && src.flipX);

        float s = width * scale / sprite.bounds.size.x;
        float flicker = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 40f);
        burst.transform.localScale = new Vector3(s, s * flicker, 1f);
        burst.transform.position = Feet() + (Vector3)offset;
        Color c = Color.white;
        c.a = Mathf.Clamp01(alpha) * maxAlpha;
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
