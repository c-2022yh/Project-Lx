using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 배경 분위기 연출.
/// 1) 바닥에 깔린 오로라 빛 여러 겹(glows)이 서로 박자를 달리해 밝아졌다 어두워지고, 좌우로 살짝 흔들린다.
/// 2) 반딧불 같은 빛 알갱이들이 화면 양옆에서 나타나 안쪽으로 떠오르다 사라진다.
/// 이 오브젝트의 RectTransform 크기 안에서만 움직인다. 시간 정지(timeScale 0)와 무관하게 돈다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TitleAmbience : MonoBehaviour
{
    [Header("오로라 빛 (자식 이미지들)")]
    [SerializeField] private Graphic[] glows;
    [Tooltip("밝기가 오르내리는 정도. 0.6이면 원래 밝기의 40%~160% 사이를 오간다")]
    [SerializeField, Range(0f, 1f)] private float glowPulse = 0.6f;
    [Tooltip("한 번 밝아졌다 어두워지는 데 걸리는 시간(초)")]
    [SerializeField] private float glowPeriod = 9f;
    [Tooltip("좌우로 흔들리는 거리(픽셀). 이미지가 화면보다 이만큼 넓어야 가장자리가 안 보인다")]
    [SerializeField] private float glowSway = 30f;

    [Header("반딧불")]
    [SerializeField] private Sprite fireflySprite;
    [SerializeField] private int fireflyCount = 28;
    [SerializeField] private Vector2 fireflySize = new Vector2(22f, 48f);
    [SerializeField] private Color fireflyColor = new Color(0.74f, 0.80f, 0.98f, 1f);
    [Tooltip("양옆 어디까지에서 나타나는지 (화면 너비 비율)")]
    [SerializeField, Range(0.05f, 0.5f)] private float sideBand = 0.18f;
    [Tooltip("나타나는 높이 범위 (0 = 화면 아래, 1 = 위)")]
    [SerializeField] private Vector2 spawnHeight = new Vector2(0.05f, 0.7f);
    [Tooltip("안쪽으로 들어오는 속도 (픽셀/초)")]
    [SerializeField] private Vector2 inwardSpeed = new Vector2(10f, 30f);
    [Tooltip("위로 떠오르는 속도 (픽셀/초)")]
    [SerializeField] private float riseSpeed = 8f;
    [Tooltip("흔들리며 떠다니는 정도 (픽셀/초)")]
    [SerializeField] private float wanderSpeed = 16f;
    [Tooltip("한 알갱이가 보이는 시간 (초)")]
    [SerializeField] private Vector2 lifetime = new Vector2(7f, 14f);

    private struct Firefly
    {
        public RectTransform rect;
        public Image image;
        public Vector2 pos;
        public Vector2 velocity;
        public float age;
        public float life;
        public float seed;
        public float blinkSpeed;
        public float blinkPhase;
        public float brightness;
    }

    private readonly List<Firefly> flies = new List<Firefly>();
    private float[] glowBaseAlpha;
    private Vector2[] glowBasePos;
    private RectTransform self;
    private bool spawned;

    private void Awake()
    {
        self = (RectTransform)transform;

        int n = glows != null ? glows.Length : 0;
        glowBaseAlpha = new float[n];
        glowBasePos = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            if (glows[i] == null) continue;
            glowBaseAlpha[i] = glows[i].color.a;
            glowBasePos[i] = glows[i].rectTransform.anchoredPosition;
        }
    }

    // 레이아웃(배경 비율 맞춤)이 끝나기 전에는 크기가 0일 수 있어서, 크기가 잡힌 뒤에 만든다.
    private void SpawnAll()
    {
        if (fireflySprite == null) return;
        for (int i = 0; i < fireflyCount; i++)
        {
            Firefly f = CreateFirefly(i);
            Respawn(ref f);
            // 처음부터 화면에 고르게 퍼져 있도록 나이를 미리 먹여 둔다
            float pre = Random.Range(0f, f.life);
            f.age = pre;
            f.pos += f.velocity * pre;
            flies.Add(f);
        }
        spawned = true;
    }

    private Firefly CreateFirefly(int index)
    {
        GameObject go = new GameObject("Firefly_" + index, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        Image image = go.GetComponent<Image>();
        image.sprite = fireflySprite;
        image.raycastTarget = false;
        image.color = Color.clear;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        return new Firefly { rect = rt, image = image };
    }

    private void Respawn(ref Firefly f)
    {
        Rect r = self.rect;
        bool left = Random.value < 0.5f;
        float band = r.width * sideBand;

        float x = left ? Random.Range(r.xMin - 30f, r.xMin + band) : Random.Range(r.xMax - band, r.xMax + 30f);
        float y = Mathf.Lerp(r.yMin, r.yMax, Random.Range(spawnHeight.x, spawnHeight.y));

        float size = Random.Range(fireflySize.x, fireflySize.y);
        f.rect.sizeDelta = new Vector2(size, size);

        f.pos = new Vector2(x, y);
        float inward = Random.Range(inwardSpeed.x, inwardSpeed.y);
        f.velocity = new Vector2(left ? inward : -inward, riseSpeed * Random.Range(0.5f, 1.5f));
        f.age = 0f;
        f.life = Random.Range(lifetime.x, lifetime.y);
        f.seed = Random.Range(0f, 1000f);
        f.blinkSpeed = Random.Range(0.25f, 0.6f);
        f.blinkPhase = Random.Range(0f, Mathf.PI * 2f);
        // 큰 알갱이는 조금 흐리게: 가까이서 흐릿하게 보이는 느낌
        f.brightness = Mathf.Lerp(1f, 0.55f, Mathf.InverseLerp(fireflySize.x, fireflySize.y, size));
    }

    private void Update()
    {
        float t = Time.unscaledTime;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

        // 겹마다 박자와 속도를 어긋나게 해서, 밝은 곳이 이리저리 옮겨 다니는 것처럼 보이게 한다
        int count = glowBaseAlpha.Length;
        for (int i = 0; i < count; i++)
        {
            if (glows[i] == null) continue;
            float period = glowPeriod * (1f + 0.23f * i);
            float wave = Mathf.Sin((t / period + (float)i / Mathf.Max(1, count)) * Mathf.PI * 2f);
            Color c = glows[i].color;
            c.a = glowBaseAlpha[i] * (1f + glowPulse * wave);
            glows[i].color = c;

            float sway = (Mathf.PerlinNoise(i * 7.3f, t * 0.05f) * 2f - 1f) * glowSway;
            glows[i].rectTransform.anchoredPosition = glowBasePos[i] + new Vector2(sway, 0f);
        }

        if (!spawned)
        {
            if (self.rect.width < 1f) return;
            SpawnAll();
        }

        for (int i = 0; i < flies.Count; i++)
        {
            Firefly f = flies[i];

            f.age += dt;
            if (f.age >= f.life) Respawn(ref f);

            // 펄린 노이즈로 부드럽게 흔들리는 움직임
            float nx = Mathf.PerlinNoise(f.seed, t * 0.2f) * 2f - 1f;
            float ny = Mathf.PerlinNoise(f.seed + 31.7f, t * 0.2f) * 2f - 1f;
            f.pos += (f.velocity + new Vector2(nx, ny) * wanderSpeed) * dt;

            // 나타났다가 사라지는 전체 밝기 × 반딧불 깜빡임
            float lifeFade = Mathf.Sin(Mathf.Clamp01(f.age / f.life) * Mathf.PI);
            float s = Mathf.Sin(t * f.blinkSpeed * Mathf.PI * 2f + f.blinkPhase) * 0.5f + 0.5f;
            float alpha = lifeFade * (0.3f + 0.7f * s * s) * f.brightness;

            Color c = fireflyColor;
            c.a *= alpha;
            f.image.color = c;
            f.rect.anchoredPosition = f.pos;
            f.rect.localScale = Vector3.one * (0.85f + 0.25f * s);

            flies[i] = f;
        }
    }
}
