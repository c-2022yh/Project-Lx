using UnityEngine;

/// <summary>
/// 보스 연출용 그림 한 장. 정해진 시간 동안 커지거나·움직이거나·흐려지다가 스스로 사라진다.
/// BossFx가 만들어서 값을 채운다. (먼지, 고리, 빛 알갱이, 빛살 등)
/// </summary>
public class BossFxSprite : MonoBehaviour
{
    public SpriteRenderer sr;
    public Color color = Color.white;
    public float delay;
    public float life = 1f;
    public bool unscaled;
    public Vector3 scaleFrom = Vector3.one;
    public Vector3 scaleTo = Vector3.one;
    public float alphaFrom = 1f;
    public float alphaTo;
    public float fadeIn;
    public Vector2 velocity;
    public float gravity;
    public float drag;
    public float spin;

    private float t;

    public static BossFxSprite Create(string name, Sprite sprite, Vector3 position, string sortingLayer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        BossFxSprite fx = go.AddComponent<BossFxSprite>();
        fx.sr = go.AddComponent<SpriteRenderer>();
        fx.sr.sprite = sprite;
        fx.sr.sharedMaterial = BossFxArt.Unlit;
        fx.sr.sortingLayerName = sortingLayer;
        fx.sr.sortingOrder = order;
        fx.sr.enabled = false;
        return fx;
    }

    private void Start()
    {
        transform.localScale = scaleFrom;
        Apply(0f);
    }

    private void Update()
    {
        float dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        if (delay > 0f)
        {
            delay -= dt;
            sr.enabled = false;
            return;
        }

        sr.enabled = true;
        t += dt;

        velocity.y -= gravity * dt;
        if (drag > 0f) velocity *= Mathf.Clamp01(1f - drag * dt);
        transform.position += (Vector3)(velocity * dt);
        if (spin != 0f) transform.Rotate(0f, 0f, spin * dt);

        float k = Mathf.Clamp01(t / Mathf.Max(0.01f, life));
        Apply(k);
        if (t >= life) Destroy(gameObject);
    }

    private void Apply(float k)
    {
        float ease = 1f - (1f - k) * (1f - k);
        transform.localScale = Vector3.LerpUnclamped(scaleFrom, scaleTo, ease);

        float a = Mathf.Lerp(alphaFrom, alphaTo, k);
        if (fadeIn > 0f && t < fadeIn) a *= t / fadeIn;
        Color c = color;
        c.a *= a;
        sr.color = c;
    }
}
