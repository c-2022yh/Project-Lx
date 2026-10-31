using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// 각성 중 캐릭터 실루엣을 따라 빛나는 아우라 (빛나는 검처럼 몸 둘레가 빛나고, 위로 불꽃·연기 자락이 피어오름).
///
///   모으기  Q를 누르면 몸에 바짝 붙은 빛만 생김
///   방출    도입이 끝날 때 자락이 위로 확 솟구치고 가장 밝아짐
///   유지    각성 중에는 일렁이며 계속 피어오름
///   꺼짐    기존 각성 연출과 같은 박자로 사라짐
///
/// 기존 PlayerAwakeningVisual의 부채꼴 줄기(아우라)는 끈다 (몸 테두리 발광·빛·블룸·화면 테두리는 그대로).
///   (PlayerAwakeningVisual 파일과 플레이어 프리팹은 고치지 않고, 실행 중에만 enableAura를 끈다.)
/// 색은 기존 각성 연출의 색(보주 유물 색 포함)을 따른다.
/// 셰이더: Resources/Presentation/Awakening/AwakeningSilhouetteAura. 씬이 열릴 때 플레이어에 스스로 붙는다.
/// </summary>
[DisallowMultipleComponent]
public class AwakeningSilhouetteAura : MonoBehaviour
{
    [Header("모양 (월드 칸, 캐릭터 키 약 2.7)")]
    [Tooltip("몸에 바짝 붙은 빛의 두께")]
    [SerializeField, Min(0.01f)] private float nearRadius = 0.06f;
    [Tooltip("넓게 퍼지는 빛의 두께")]
    [SerializeField, Min(0.01f)] private float farRadius = 0.45f;
    [Tooltip("위로 피어오르는 자락 높이")]
    [SerializeField, Min(0f)] private float riseHeight = 1.3f;

    [Header("밝기")]
    [SerializeField, Min(0f)] private float intensity = 2.2f;
    [SerializeField, Range(0f, 1f)] private float opacity = 0.85f;
    [SerializeField, Range(0f, 1f)] private float sparkle = 0.6f;
    [Tooltip("몸 테두리 흰빛 정도 (0 = 흰빛 없음)")]
    [SerializeField, Range(0f, 1f)] private float whiteCore = 0.35f;
    [Tooltip("흐르는 빠르기")]
    [SerializeField, Min(0f)] private float flowSpeed = 1f;

    [Header("색 (기존 각성 연출 색을 못 읽을 때)")]
    [SerializeField] private Color fallbackColor = new Color(0.3f, 0.62f, 1f, 1f);

    [Tooltip("기존 부채꼴 줄기 아우라를 끈다")]
    [SerializeField] private bool hideOldAura = true;

    private const string ShaderPath = "Presentation/Awakening/AwakeningSilhouetteAura";

    private Player player;
    private PlayerAwakening awakening;
    private PlayerAwakeningVisual visual;
    private MeshRenderer quad;
    private Mesh mesh;
    private Material material;
    private MaterialPropertyBlock props;
    private float weight, clock;
    private bool oldAuraHidden;

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int AuraColorId = Shader.PropertyToID("_AuraColor");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    private static readonly int NearRadiusId = Shader.PropertyToID("_NearRadius");
    private static readonly int FarRadiusId = Shader.PropertyToID("_FarRadius");
    private static readonly int RiseId = Shader.PropertyToID("_Rise");
    private static readonly int SparkleId = Shader.PropertyToID("_Sparkle");
    private static readonly int WhiteId = Shader.PropertyToID("_White");
    private static readonly int UvRectId = Shader.PropertyToID("_SpriteUVRect");
    private static readonly int SpriteRectId = Shader.PropertyToID("_SpriteRectInQuad");
    private static readonly int QuadSizeId = Shader.PropertyToID("_QuadSize");
    private static readonly int FlipId = Shader.PropertyToID("_Flip");
    private static readonly int TimeId = Shader.PropertyToID("_AuraTime");
    private static readonly int FlashId = Shader.PropertyToID("_Flash");

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
        if (p != null && p.GetComponent<AwakeningSilhouetteAura>() == null) p.gameObject.AddComponent<AwakeningSilhouetteAura>();
    }

    // ── 준비 ───────────────────────────────

    private void Start()
    {
        player = GetComponent<Player>();
        awakening = GetComponent<PlayerAwakening>();
        visual = GetComponent<PlayerAwakeningVisual>();
        Shader shader = Resources.Load<Shader>(ShaderPath);
        if (player == null || awakening == null || shader == null || !shader.isSupported)
        {
            Debug.LogWarning($"[각성 아우라] 꺼짐: PlayerAwakening {(awakening != null ? "있음" : "없음")}, 셰이더 {(shader == null ? "없음" : shader.isSupported ? "있음" : "지원 안 됨")}");
            enabled = false;
            return;
        }
        material = new Material(shader) { hideFlags = HideFlags.DontSave };
        props = new MaterialPropertyBlock();
        CreateQuad();
        if (hideOldAura) HideOldAura();
        Debug.Log("[각성 아우라] 플레이어에 붙었습니다." + (oldAuraHidden ? " 기존 줄기 아우라는 껐습니다." : ""));
    }

    private void CreateQuad()
    {
        GameObject go = new GameObject("Awakening Silhouette Aura (Runtime)");
        mesh = new Mesh { name = "Awakening Silhouette Aura Quad", hideFlags = HideFlags.DontSave };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f)
        };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        quad = go.AddComponent<MeshRenderer>();
        quad.sharedMaterial = material;
        quad.shadowCastingMode = ShadowCastingMode.Off;
        quad.receiveShadows = false;
        quad.lightProbeUsage = LightProbeUsage.Off;
        quad.reflectionProbeUsage = ReflectionProbeUsage.Off;
        quad.enabled = false;
    }

    // 기존 부채꼴 줄기만 끈다 (파일은 그대로, 실행 중에만)
    private void HideOldAura()
    {
        if (visual == null) return;
        FieldInfo f = typeof(PlayerAwakeningVisual).GetField("enableAura", BindingFlags.Instance | BindingFlags.NonPublic);
        if (f == null || f.FieldType != typeof(bool)) return;
        f.SetValue(visual, false);
        oldAuraHidden = true;
    }

    private void OnDestroy()
    {
        if (quad != null) Destroy(quad.gameObject);
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }

    // ── 매 프레임 ──────────────────────────

    private void LateUpdate()
    {
        SpriteRenderer src = player.sr;
        bool active = visual != null && visual.isActiveAndEnabled
            ? visual.IsVisualActive
            : awakening.IsAwakening || awakening.IsAwakened;
        float dt = Time.unscaledDeltaTime;

        if (visual != null && visual.isActiveAndEnabled) weight = visual.ScreenEffectWeight;
        else weight = Mathf.MoveTowards(weight, active ? 1f : 0f, dt / 0.3f);

        if (weight <= 0.001f || src == null || src.sprite == null || !src.enabled
            || !src.gameObject.activeInHierarchy || src.color.a <= 0f)
        {
            quad.enabled = false;
            if (weight <= 0.001f) clock = 0f;
            return;
        }

        clock += dt * flowSpeed;

        // 도입 박자: 모을 때는 몸에 붙고, 터질 때 위로 솟구친다
        float rise = 1f, flash = 0f;
        if (visual != null && visual.isActiveAndEnabled && visual.UsesIntroSequence && active)
        {
            float e = visual.VisualIntroElapsed;
            float c = visual.IntroChargeEnd, r = visual.IntroReleaseEnd;
            if (e < c) rise = Mathf.Lerp(0.1f, 0.35f, e / Mathf.Max(0.01f, c));
            else if (e < r) rise = Mathf.Lerp(0.35f, 1.4f, (e - c) / Mathf.Max(0.01f, r - c));
            else rise = Mathf.Lerp(1.4f, 1f, Mathf.Clamp01((e - r) / 0.35f));
            flash = visual.IntroFlashStrength;
        }

        // 지금 캐릭터 그림의 자리 (월드)
        Sprite sp = src.sprite;
        Texture2D tex = sp.texture;
        if (tex == null) { quad.enabled = false; return; }
        float ppu = sp.pixelsPerUnit;
        Rect rect = sp.rect;
        Rect texRect;
        try { texRect = sp.textureRect; }
        catch { texRect = rect; }
        Vector4 uvRect = new Vector4(texRect.xMin / tex.width, texRect.yMin / tex.height,
                                     texRect.xMax / tex.width, texRect.yMax / tex.height);

        Transform st = src.transform;
        Vector3 ls = st.lossyScale;
        Vector2 localMin = -sp.pivot / ppu;
        Vector2 localSize = rect.size / ppu;
        Vector3 localCenter = new Vector3(localMin.x + localSize.x * 0.5f, localMin.y + localSize.y * 0.5f, 0f);
        if (src.flipX) localCenter.x = -localCenter.x;
        if (src.flipY) localCenter.y = -localCenter.y;
        Vector3 center = st.TransformPoint(localCenter);
        float w = localSize.x * Mathf.Abs(ls.x);
        float h = localSize.y * Mathf.Abs(ls.y);
        bool flip = src.flipX ^ (ls.x < 0f);

        float riseNow = riseHeight * rise;
        float mx = farRadius + 0.55f;
        float mb = farRadius + 0.1f;
        float mt = riseNow + farRadius * 2f + 0.4f;
        Vector2 size = new Vector2(w + mx * 2f, h + mb + mt);
        Vector2 quadMin = new Vector2(center.x - w * 0.5f - mx, center.y - h * 0.5f - mb);

        quad.transform.position = new Vector3(quadMin.x + size.x * 0.5f, quadMin.y + size.y * 0.5f, st.position.z);
        quad.transform.rotation = Quaternion.identity;
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);
        quad.sortingLayerID = src.sortingLayerID;
        quad.sortingOrder = src.sortingOrder - 1;

        props.SetTexture(MainTexId, tex);
        props.SetColor(AuraColorId, ResolveColor());
        props.SetFloat(IntensityId, intensity);
        props.SetFloat(OpacityId, opacity * weight * src.color.a);
        props.SetFloat(NearRadiusId, nearRadius);
        props.SetFloat(FarRadiusId, farRadius);
        props.SetFloat(RiseId, riseNow);
        props.SetFloat(SparkleId, sparkle);
        props.SetFloat(WhiteId, whiteCore);
        props.SetVector(UvRectId, uvRect);
        props.SetVector(SpriteRectId, new Vector4(mx, mb, w, h));
        props.SetVector(QuadSizeId, new Vector4(size.x, size.y, 0f, 0f));
        props.SetFloat(FlipId, flip ? 1f : 0f);
        props.SetFloat(TimeId, clock);
        props.SetFloat(FlashId, flash);
        quad.SetPropertyBlock(props);
        quad.enabled = true;
    }

    // 기존 각성 연출 색(보주 유물 색 포함)을 밝기 1로 맞춰 쓴다
    private Color ResolveColor()
    {
        Color c = visual != null ? visual.CurrentGlowColor : fallbackColor;
        float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        if (m <= 0.0001f) c = fallbackColor;
        else c = new Color(c.r / m, c.g / m, c.b / m, 1f);
        return Color.Lerp(c, Color.white, 0.12f);
    }
}
