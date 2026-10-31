using UnityEngine;

/// <summary>
/// 동굴 안개. 화면 전체에 옅은 안개가 천천히 흘러가고, 바닥 쪽에는 조금 더 진한 낮은 안개가 깔린다.
///
///   뒤 안개  배경과 땅 사이 (BackGround 레이어, 배경 바로 앞). 화면 전체를 덮는 옅은 안개
///   앞 안개  캐릭터 앞 (GroundCover 레이어). 화면 아래쪽에만 깔리는 낮은 안개
///
/// 안개 그림은 코드로 만든다 (가로로 끊김 없이 이어지는 구름 무늬). 카메라를 따라다니며
/// 카메라보다 조금 느리게 + 저절로 천천히 흘러서 깊이감이 난다.
/// 출구(오른쪽)로 갈수록 옅어진다. 값은 Play 중 인스펙터에서 바꿔 볼 수 있다.
/// 튜토리얼 맵 빌더가 Tutorial_Fog 오브젝트에 붙인다.
/// </summary>
[DefaultExecutionOrder(900)]   // 카메라가 움직인 다음에 자리를 정한다
public class CaveFog : MonoBehaviour
{
    [Header("색")]
    [SerializeField] private Color fogColor = new Color(0.72f, 0.82f, 1f, 1f);

    [Header("뒤 안개 (배경 앞, 화면 전체)")]
    [SerializeField, Range(0f, 1f)] private float backAlpha = 0.28f;
    [SerializeField] private float backDrift = 0.25f;            // 저절로 흐르는 빠르기 (초당 칸)
    [SerializeField, Range(0f, 1f)] private float backFollow = 0.3f;   // 카메라 움직임에 따라 흐르는 정도

    [Header("앞 안개 (캐릭터 앞, 바닥 쪽)")]
    [SerializeField, Range(0f, 1f)] private float frontAlpha = 0.22f;
    [SerializeField] private float frontDrift = 0.6f;
    [SerializeField, Range(0f, 1f)] private float frontFollow = 0.15f;
    [Tooltip("화면 높이에 대한 낮은 안개 높이")]
    [SerializeField, Range(0.1f, 1f)] private float frontHeight = 0.38f;

    [Header("출구 쪽으로 갈수록 옅게 (월드 x)")]
    [SerializeField] private float thickX = 20f;
    [SerializeField] private float thinX = 130f;
    [SerializeField, Range(0f, 1f)] private float thinMultiplier = 0.45f;

    private Camera cam;
    private Layer back, front;

    private class Layer
    {
        public SpriteRenderer a, b;
        public float scroll;
    }

    private void Start()
    {
        Sprite fog = MakeFogSprite();
        back = MakeLayer("Fog_Back", fog, "BackGround", 50);
        front = MakeLayer("Fog_Front", fog, "GroundCover", 5);
    }

    private Layer MakeLayer(string name, Sprite sprite, string sortingLayer, int order)
    {
        Layer l = new Layer();
        l.a = MakeRenderer(name + "_A", sprite, sortingLayer, order);
        l.b = MakeRenderer(name + "_B", sprite, sortingLayer, order);
        return l;
    }

    private SpriteRenderer MakeRenderer(string name, Sprite sprite, string sortingLayer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = BossFxArt.Unlit;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        return sr;
    }

    private void LateUpdate()
    {
        if (back == null) return;
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        float viewH = cam.orthographic ? cam.orthographicSize * 2f : 14f;
        float viewW = viewH * cam.aspect;
        Vector3 c = cam.transform.position;
        float dt = Time.deltaTime;

        float k = Mathf.InverseLerp(thickX, thinX, c.x);
        float density = Mathf.Lerp(1f, thinMultiplier, k);

        // 뒤 안개: 화면 전체
        back.scroll += backDrift * dt;
        Place(back, c, viewW, viewH * 1.1f, 0f, backFollow, backAlpha * density);

        // 앞 안개: 화면 아래쪽
        front.scroll += frontDrift * dt;
        float h = viewH * frontHeight;
        Place(front, c, viewW, h, -viewH * 0.5f + h * 0.45f, frontFollow, frontAlpha * density);
    }

    // 같은 그림 두 장을 이어 붙여 흘려보낸다. 한 장 폭이 화면보다 넓어서 끊긴 곳이 안 보인다.
    private void Place(Layer l, Vector3 cam, float viewW, float height, float yOffset, float follow, float alpha)
    {
        float w = viewW + 2f;
        Vector2 size = l.a.sprite.bounds.size;
        Vector3 scale = new Vector3(w / size.x, height / size.y, 1f);
        l.a.transform.localScale = scale;
        l.b.transform.localScale = scale;

        float offset = Mathf.Repeat(cam.x * follow + l.scroll, w);
        float left = cam.x - viewW * 0.5f - offset;          // 첫 장의 왼쪽 끝
        float y = cam.y + yOffset;
        l.a.transform.position = new Vector3(left + w * 0.5f, y, 0f);
        l.b.transform.position = new Vector3(left + w * 1.5f, y, 0f);

        Color col = fogColor;
        col.a = alpha;
        l.a.color = col;
        l.b.color = col;
    }

    // 가로로 끊김 없이 이어지는 구름 무늬. 위아래 끝은 흐리게 사라진다.
    private static Sprite MakeFogSprite()
    {
        const int W = 256, H = 128;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
        {
            name = "CaveFog",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };

        Color32[] px = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            float v = (y + 0.5f) / H;
            float edge = Mathf.Sin(v * Mathf.PI);                  // 위아래 끝 0
            edge *= edge;
            for (int x = 0; x < W; x++)
            {
                // 왼쪽 끝과 오른쪽 끝이 같은 값이 되도록 두 번 샘플해서 섞는다
                float t = (float)x / W;
                float n = Mathf.Lerp(Fbm(x, y), Fbm(x - W, y), t);
                float a = Mathf.Clamp01((n - 0.35f) * 1.8f) * edge;
                px[y * W + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);

        Sprite s = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), W, 0, SpriteMeshType.FullRect);
        s.name = "CaveFog";
        s.hideFlags = HideFlags.DontSave;
        return s;
    }

    private static float Fbm(float x, float y)
    {
        float sum = 0f, amp = 0.5f, freq = 1f / 64f;
        for (int i = 0; i < 4; i++)
        {
            sum += amp * Mathf.PerlinNoise(x * freq + 31.7f, y * freq * 1.6f + 11.3f);
            amp *= 0.5f;
            freq *= 2f;
        }
        return sum / 0.9375f;
    }
}
