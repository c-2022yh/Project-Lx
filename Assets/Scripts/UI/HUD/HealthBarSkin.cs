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
/// 보주를 얻으면 보주 오른쪽 옆으로 옮겨가서 보주 가운데 높이에 붙는다 (왼쪽 끝은 보주 뒤로 숨음).
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

    [Header("보주가 있을 때: 보주 옆에 붙기")]
    [Tooltip("보주 반지름 대비, 프레임 왼쪽 끝이 보주 가운데에서 오른쪽으로 떨어진 정도 (보주가 왼쪽 끝을 덮음)")]
    [SerializeField, Range(-1f, 1.2f)] private float orbOverlap = 0.35f;
    [SerializeField] private Vector2 orbOffset = Vector2.zero;
    [Tooltip("자리 옮길 때 걸리는 시간 (초)")]
    [SerializeField] private float moveTime = 0.25f;

    // 프레임 그림(1329×172) 안에서 채움 창의 자리
    private const float ArtW = 1329f, ArtH = 172f;
    private static readonly Rect ArtWindow = new Rect(45f, 48f, 1239f, 76f);   // x, 위에서 y, 폭, 높이

    private Image fill, trail, frame, slot;
    private RectTransform parentRect, orbRoot, orbCircle;
    private PlayerEnergy energy;
    private Vector2 frameSize, winOffset, winSize, currentPos;
    private bool orderedBehindOrb, placed;
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

        parentRect = parent as RectTransform;
        float k = frameWidth / ArtW;
        frameSize = new Vector2(frameWidth, ArtH * k);
        winOffset = new Vector2(ArtWindow.x * k, -ArtWindow.y * k);
        winSize = new Vector2(ArtWindow.width * k, ArtWindow.height * k);
        Vector2 winPos = framePosition + winOffset;
        currentPos = framePosition;

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

    // ── 보주 옆으로 옮기기 ───────────────────

    private void LateUpdate()
    {
        if (frame == null || parentRect == null) return;
        Vector2 target = framePosition;
        // 보주가 없어도 보주 자리 옆에 둔다 (보주를 얻어도 체력바가 움직이지 않게)
        if (FindOrb() && OrbCenter(out Vector2 c, out float r))
        {
            // 프레임 세로 가운데 = 보주 가운데, 왼쪽 끝은 보주 뒤로
            target = new Vector2(c.x + r * orbOverlap, c.y + frameSize.y * 0.5f) + orbOffset;
            if (!orderedBehindOrb) PutBehindOrb();
        }
        currentPos = placed && moveTime > 0f
            ? Vector2.Lerp(currentPos, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 4.6f / moveTime))
            : target;
        placed = true;
        Layout(currentPos);
    }

    private void Layout(Vector2 topLeft)
    {
        Vector2 win = topLeft + winOffset;
        if (slot != null) slot.rectTransform.anchoredPosition = win;
        fill.rectTransform.anchoredPosition = win;
        trail.rectTransform.anchoredPosition = win;
        frame.rectTransform.anchoredPosition = topLeft;
    }

    private bool HasOrb()
    {
        if (energy == null) energy = FindAnyObjectByType<PlayerEnergy>();
        return energy != null && energy.HasOrb;
    }

    private bool FindOrb()
    {
        if (orbRoot != null) return true;
        OrbHUD orb = FindAnyObjectByType<OrbHUD>(FindObjectsInactive.Include);
        if (orb == null) return false;
        orbRoot = orb.transform as RectTransform;
        Transform circle = orb.transform.Find("OrbFrame");
        orbCircle = circle != null ? circle as RectTransform : orbRoot;
        return orbRoot != null;
    }

    // 보주 동그라미 가운데(체력바 기준 좌표: 부모 왼쪽 위에서)와 반지름
    private bool OrbCenter(out Vector2 center, out float radius)
    {
        center = Vector2.zero;
        radius = 0f;
        if (orbCircle == null || !orbCircle.gameObject.activeInHierarchy) return false;
        Vector3[] w = new Vector3[4];
        orbCircle.GetWorldCorners(w);
        Vector2 a = parentRect.InverseTransformPoint(w[0]);
        Vector2 b = parentRect.InverseTransformPoint(w[2]);
        Rect pr = parentRect.rect;
        Vector2 mid = (a + b) * 0.5f;
        center = new Vector2(mid.x - pr.xMin, mid.y - pr.yMax);
        radius = Mathf.Abs(b.x - a.x) * 0.5f;
        return radius > 1f;
    }

    // 체력바가 보주 뒤에 그려지게 (같은 부모일 때만)
    private void PutBehindOrb()
    {
        orderedBehindOrb = true;
        if (orbRoot == null || orbRoot.parent != fill.transform.parent) return;
        Transform[] order = { slot != null ? slot.transform : null, trail.transform, fill.transform, frame.transform };
        // 바탕 → 잔상 → 채움 → 프레임 순서를 지키면서 보주 앞(뒤에 그려지도록)으로 옮긴다
        foreach (Transform t in order)
        {
            if (t == null) continue;
            if (t.GetSiblingIndex() > orbRoot.GetSiblingIndex()) t.SetSiblingIndex(orbRoot.GetSiblingIndex());
        }
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
