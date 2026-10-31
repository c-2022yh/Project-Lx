using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 체력바 새 디자인 (은빛 프레임 + 붉은 채움 + 방금 잃은 체력이 천천히 줄어드는 밝은 잔상).
///
///   HealthBar_BG     프레임 안쪽 어두운 바탕
///   HealthBar_Trail  방금 잃은 체력 (맞으면 0.4초 머물렀다가 천천히 줄어듦)   ← 실행 중에 만듦
///   HealthBar_Fill   현재 체력 (HealthHUD가 fillAmount를 바꿈)
///   HealthBar_Frame  은빛 프레임                                              ← 실행 중에 만듦
///
/// HUD_Canvas 프리팹·HealthHUD는 고치지 않는다. HealthHUD가 붙은 오브젝트를 찾아 실행 중에만 모양을 바꾼다.
/// 그림: Resources/Presentation/HUD/HUD_HpFrame, HUD_HpFill
/// </summary>
[DisallowMultipleComponent]
public class HealthBarSkin : MonoBehaviour
{
    [Header("자리 (1920×1080 기준, 화면 왼쪽 위에서)")]
    [SerializeField] private Vector2 framePosition = new Vector2(16f, -26f);
    [SerializeField] private float frameWidth = 380f;

    [Header("잔상")]
    [SerializeField] private Color trailColor = new Color(1f, 0.92f, 0.88f, 0.85f);
    [SerializeField] private float trailHold = 0.4f;
    [Tooltip("초당 줄어드는 양 (전체 = 1)")]
    [SerializeField] private float trailSpeed = 0.8f;

    [SerializeField] private Color slotColor = new Color(0.04f, 0.05f, 0.09f, 0.92f);

    // 프레임 그림(1329×172) 안에서 채움 창의 자리
    private const float ArtW = 1329f, ArtH = 172f;
    private static readonly Rect ArtWindow = new Rect(45f, 48f, 1239f, 76f);   // x, 위에서 y, 폭, 높이

    private Image fill, trail, frame, slot;
    private float trailAmount = 1f, holdTimer, lastFill = 1f;

    private static float nextScan;

    // ── 붙이기 ─────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameObject runner = new GameObject("HealthBarSkin (Runtime)") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(runner);
        runner.AddComponent<Scanner>();
        Scan();
    }

    private static void OnSceneLoaded(Scene s, LoadSceneMode m) => Scan();

    private static void Scan()
    {
        foreach (HealthHUD h in FindObjectsByType<HealthHUD>(FindObjectsInactive.Include))
            if (h.GetComponent<HealthBarSkin>() == null) h.gameObject.AddComponent<HealthBarSkin>();
    }

    // UI가 늦게 생기는 경우를 위해 1초에 한 번 다시 찾는다
    private class Scanner : MonoBehaviour
    {
        private void Update()
        {
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 1f;
            Scan();
        }
    }

    // ── 모양 바꾸기 ─────────────────────────

    private void Start()
    {
        fill = GetComponent<Image>();
        Sprite frameArt = Resources.Load<Sprite>("Presentation/HUD/HUD_HpFrame");
        Sprite fillArt = Resources.Load<Sprite>("Presentation/HUD/HUD_HpFill");
        if (fill == null || frameArt == null || fillArt == null)
        {
            Debug.LogWarning("[체력바] 새 디자인 그림을 못 찾아서 원래 모양으로 둡니다.");
            enabled = false;
            return;
        }

        Transform parent = fill.transform.parent;
        Transform bg = parent != null ? parent.Find("HealthBar_BG") : null;
        slot = bg != null ? bg.GetComponent<Image>() : null;

        float k = frameWidth / ArtW;
        Vector2 frameSize = new Vector2(frameWidth, ArtH * k);
        Vector2 winPos = framePosition + new Vector2(ArtWindow.x * k, -ArtWindow.y * k);
        Vector2 winSize = new Vector2(ArtWindow.width * k, ArtWindow.height * k);

        // 바탕
        if (slot != null)
        {
            slot.sprite = null;
            slot.color = slotColor;
            slot.raycastTarget = false;
            Place(slot.rectTransform, winPos, winSize);
        }

        // 현재 체력
        fill.sprite = fillArt;
        fill.color = Color.white;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.raycastTarget = false;
        Place(fill.rectTransform, winPos, winSize);

        // 잔상 (채움 바로 뒤)
        trail = MakeImage("HealthBar_Trail", parent, fillArt);
        trail.color = trailColor;
        trail.type = Image.Type.Filled;
        trail.fillMethod = Image.FillMethod.Horizontal;
        trail.fillOrigin = (int)Image.OriginHorizontal.Left;
        Place(trail.rectTransform, winPos, winSize);
        trail.transform.SetSiblingIndex(fill.transform.GetSiblingIndex());
        trailAmount = lastFill = fill.fillAmount;
        trail.fillAmount = trailAmount;

        // 프레임 (맨 위)
        frame = MakeImage("HealthBar_Frame", parent, frameArt);
        Place(frame.rectTransform, framePosition, frameSize);
        frame.transform.SetSiblingIndex(fill.transform.GetSiblingIndex() + 1);
    }

    private static Image MakeImage(string name, Transform parent, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent != null ? parent.gameObject.layer : 5;
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    // 왼쪽 위 기준으로 자리와 크기를 정한다
    private static void Place(RectTransform r, Vector2 topLeft, Vector2 size)
    {
        r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
        r.pivot = new Vector2(0f, 1f);
        r.sizeDelta = size;
        r.anchoredPosition = topLeft;
        r.localScale = Vector3.one;
    }

    // ── 잔상 ───────────────────────────────

    private void Update()
    {
        if (trail == null) return;
        float now = fill.fillAmount;
        if (now < lastFill - 0.0001f) holdTimer = 0f;   // 새로 맞으면 다시 잠깐 머문다
        lastFill = now;
        if (now >= trailAmount)
        {
            trailAmount = now;   // 회복하면 바로 따라감
            holdTimer = 0f;
        }
        else if (holdTimer < trailHold)
        {
            holdTimer += Time.unscaledDeltaTime;
        }
        else
        {
            trailAmount = Mathf.MoveTowards(trailAmount, now, trailSpeed * Time.unscaledDeltaTime);
        }
        trail.fillAmount = trailAmount;
        trail.enabled = trailAmount > now + 0.001f;
    }

    private void OnDestroy()
    {
        if (trail != null) Destroy(trail.gameObject);
        if (frame != null) Destroy(frame.gameObject);
    }
}
