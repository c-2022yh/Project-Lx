using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬을 넘어갈 때 화면을 검게 덮었다가 걷어낸다.
///
/// - 어떤 방법으로 씬이 바뀌든, 새 씬은 검은 화면에서 서서히 밝아진다. (sceneLoaded에 붙어 있음)
/// - SceneFader.LoadScene(이름)으로 넘기면 떠나기 전에도 서서히 어두워진다.
///   GameFlow(타이틀/게임 시작)는 이걸 쓴다. 포탈도 이걸 쓰면 양쪽 다 부드러워진다.
///
/// 씬에도 프리팹에도 붙이지 않는다. 게임이 시작되면 스스로 생긴다.
/// </summary>
public class SceneFader : MonoBehaviour
{
    public const float FadeOutSeconds = 0.3f;
    public const float FadeInSeconds = 0.45f;

    private static SceneFader instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        GameObject go = new GameObject("[SceneFader]");
        instance = go.AddComponent<SceneFader>();
        DontDestroyOnLoad(go);

        // 첫 씬은 sceneLoaded가 이미 지나갔다. 바로 밝아지기 시작한다.
        instance.BeginFadeIn();
    }

    /// <summary>어두워지는 중이거나 씬을 넘기는 중인지.</summary>
    public static bool IsTransitioning => instance != null && instance.loading;

    /// <summary>화면이 아직 덮여 있는지(조금이라도). 지역 이름은 다 밝아진 뒤에 띄운다.</summary>
    public static bool IsCovering => instance != null && instance.group != null && instance.group.alpha > 0.01f;

    /// <summary>서서히 어두워진 뒤 씬을 넘긴다. 넘어간 씬은 알아서 밝아진다.</summary>
    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        if (instance == null) Bootstrap();

        if (instance.loading) return;   // 두 번 눌려도 한 번만 넘긴다

        instance.StopAllCoroutines();
        instance.StartCoroutine(instance.FadeOutThenLoad(sceneName));
    }

    /// <summary>
    /// 씬을 직접 불러오는 코루틴(사망 리스폰 등)에서 쓴다. 화면이 다 덮일 때까지 기다린다.
    /// 그다음 씬을 불러오면 새 씬은 알아서 밝아진다.
    /// </summary>
    public static IEnumerator CoverRoutine()
    {
        if (instance == null) Bootstrap();
        instance.StopAllCoroutines();
        instance.loading = true;
        yield return instance.Fade(instance.group.alpha, 1f, FadeOutSeconds);
    }

    private CanvasGroup group;
    private bool loading;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildView();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;

        loading = false;
        BeginFadeIn();
    }

    private void BeginFadeIn()
    {
        StopAllCoroutines();
        StartCoroutine(Fade(1f, 0f, FadeInSeconds));
    }

    private IEnumerator FadeOutThenLoad(string sceneName)
    {
        loading = true;

        yield return Fade(group.alpha, 1f, FadeOutSeconds);

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        SetAlpha(from);

        // 씬을 막 불러온 직후 몇 프레임은 로딩 시간이 한 프레임에 몰려 들어온다(수백 ms~수 초).
        // 그대로 쓰면 첫 프레임에 페이드가 끝나 버려서 안 보인다. 두 프레임 쉬고, 한 프레임 길이도 묶는다.
        yield return null;
        yield return null;

        float t = 0f;
        while (t < seconds)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

            // 처음엔 빠르게, 끝엔 천천히
            float k = Mathf.Clamp01(t / seconds);
            k = 1f - (1f - k) * (1f - k);
            SetAlpha(Mathf.Lerp(from, to, k));

            yield return null;
        }

        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        group.alpha = alpha;

        // 덮여 있는 동안만 클릭을 막는다.
        group.blocksRaycasts = alpha > 0.5f;
    }

    private void BuildView()
    {
        GameObject canvasGo = new GameObject("SceneFader_Canvas", typeof(RectTransform), typeof(Canvas),
                                             typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;   // 모든 UI 위

        group = canvasGo.GetComponent<CanvasGroup>();
        group.interactable = false;

        GameObject black = new GameObject("Black", typeof(RectTransform), typeof(Image));
        RectTransform rect = black.GetComponent<RectTransform>();
        rect.SetParent(canvasGo.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        black.GetComponent<Image>().color = new Color(0.01f, 0.02f, 0.025f, 1f);

        SetAlpha(1f);
    }
}
