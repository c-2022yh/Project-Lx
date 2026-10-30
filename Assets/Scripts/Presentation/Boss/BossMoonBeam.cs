using UnityEngine;

/// <summary>
/// 보스 위로 내려오는 달빛 기둥. 보스방 연출이 만들고 지운다.
///   Cool   싸움 시작 (차가운 달빛)
///   Warm   2페이즈 (붉게 달아오름)
///   Bright 격파 뒤 (방이 맑아짐)
/// 색은 실제 시간으로 천천히 바뀐다.
/// </summary>
public class BossMoonBeam : MonoBehaviour
{
    public enum Mood { Off, Cool, Warm, Bright }

    private const string Layer = "Effects_Back";

    private static readonly Color CoolColor = new Color(0.72f, 0.82f, 1f, 0.30f);
    private static readonly Color WarmColor = new Color(1f, 0.62f, 0.45f, 0.32f);
    private static readonly Color BrightColor = new Color(0.82f, 0.90f, 1f, 0.45f);

    private SpriteRenderer beam;
    private SpriteRenderer pool;     // 바닥에 고인 빛
    private Color target;
    private float speed = 1f;
    private bool dying;

    /// <param name="top">빛이 시작하는 곳(천장 가운데)</param>
    /// <param name="height">천장에서 바닥까지 높이</param>
    public static BossMoonBeam Create(Vector3 top, float height, float width)
    {
        GameObject go = new GameObject("Boss Moon Beam");
        go.transform.position = top;
        BossMoonBeam b = go.AddComponent<BossMoonBeam>();

        b.beam = NewRenderer("Beam", go.transform, BossFxArt.Beam, 0);
        Vector2 size = BossFxArt.Beam.bounds.size;
        b.beam.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);

        b.pool = NewRenderer("Pool", go.transform, BossFxArt.SoftDot, 1);
        b.pool.transform.localPosition = Vector3.down * height;
        b.pool.transform.localScale = new Vector3(width * 1.6f, width * 0.3f, 1f);

        b.target = new Color(CoolColor.r, CoolColor.g, CoolColor.b, 0f);
        b.Paint(b.target);
        return b;
    }

    private static SpriteRenderer NewRenderer(string name, Transform parent, Sprite sprite, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = BossFxArt.Unlit;
        sr.sortingLayerName = Layer;
        sr.sortingOrder = order;
        return sr;
    }

    public void SetMood(Mood mood, float seconds = 1.2f)
    {
        speed = 1f / Mathf.Max(0.05f, seconds);
        switch (mood)
        {
            case Mood.Cool: target = CoolColor; break;
            case Mood.Warm: target = WarmColor; break;
            case Mood.Bright: target = BrightColor; break;
            default: target = new Color(beam.color.r, beam.color.g, beam.color.b, 0f); break;
        }
    }

    /// <summary>천천히 꺼진 뒤 스스로 사라진다.</summary>
    public void FadeAndDestroy(float seconds = 0.8f)
    {
        SetMood(Mood.Off, seconds);
        dying = true;
    }

    private void Update()
    {
        Color now = beam.color;
        Color next = Color.Lerp(now, target, Mathf.Clamp01(speed * 2.5f * Time.unscaledDeltaTime));
        if (Mathf.Abs(next.a - target.a) < 0.003f) next = target;
        Paint(next);
        if (dying && next.a <= 0.003f) Destroy(gameObject);
    }

    private void Paint(Color c)
    {
        beam.color = c;
        pool.color = new Color(c.r, c.g, c.b, c.a * 0.8f);
    }
}
