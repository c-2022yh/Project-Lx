using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 보스를 쓰러뜨린 뒤 나오는 엔딩 화면 (11.01).
///
///   BossEnding.Play();
///
/// 화면이 천천히 어두워지고 "플레이해 주셔서 감사합니다"가 뜬다.
/// 잠시 뒤 아무 키나 누르면(또는 일정 시간이 지나면) 타이틀 화면으로 돌아간다.
/// 실제 시간으로 흐르고, 그동안 게임은 멈춘다.
/// </summary>
public class BossEnding : MonoBehaviour
{
    private const float DarkenSeconds = 1.8f;
    private const float TextFadeSeconds = 1.2f;
    private const float InputGuardSeconds = 2.5f;   // 글씨가 뜬 뒤 이 시간 전에는 키를 받지 않는다
    private const float AutoReturnSeconds = 12f;    // 아무것도 안 눌러도 이만큼 지나면 타이틀로

    private const string MainMessage = "플레이해 주셔서 감사합니다";
    private const string SubMessage = "Thank you for playing";
    private const string HintMessage = "아무 키나 누르면 타이틀로 돌아갑니다";

    private static BossEnding instance;

    private CanvasGroup group;
    private CanvasGroup textGroup;
    private CanvasGroup hintGroup;

    public static bool IsPlaying => instance != null;

    public static void Play()
    {
        if (instance != null) return;
        instance = Build();
        instance.StartCoroutine(instance.Run());
    }

    private IEnumerator Run()
    {
        group.blocksRaycasts = true;   // 뒤의 UI를 누르지 못하게

        // 화면이 어두워진다
        float t = 0f;
        while (t < DarkenSeconds)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Smooth(t / DarkenSeconds);
            yield return null;
        }
        group.alpha = 1f;
        Time.timeScale = 0f;   // 게임은 멈춘다 (타이틀로 갈 때 GameFlow가 1로 되돌린다)

        // 감사 문구
        t = 0f;
        while (t < TextFadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            textGroup.alpha = Smooth(t / TextFadeSeconds);
            yield return null;
        }
        textGroup.alpha = 1f;

        // 잠깐 기다린 뒤 안내 문구, 그다음부터 키를 받는다
        yield return new WaitForSecondsRealtime(InputGuardSeconds);
        float shown = 0f;
        float waited = 0f;
        while (waited < AutoReturnSeconds)
        {
            waited += Time.unscaledDeltaTime;
            shown += Time.unscaledDeltaTime;
            hintGroup.alpha = Mathf.Clamp01(shown / 0.8f) * (0.55f + 0.25f * Mathf.Sin(waited * 2.4f));
            if (AnyPressed()) break;
            yield return null;
        }

        // 글씨가 사라지고 타이틀로
        t = 0f;
        while (t < 0.8f)
        {
            t += Time.unscaledDeltaTime;
            textGroup.alpha = 1f - Smooth(t / 0.8f);
            hintGroup.alpha = Mathf.Min(hintGroup.alpha, textGroup.alpha);
            yield return null;
        }
        GameFlow.GoToTitle();
    }

    private static bool AnyPressed()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame)) return true;
        return false;
    }

    private static float Smooth(float k)
    {
        k = Mathf.Clamp01(k);
        return k * k * (3f - 2f * k);
    }

    private static BossEnding Build()
    {
        Canvas canvas = PresentationUI.NewCanvas("[BossEnding]", 900);
        BossEnding ending = canvas.gameObject.AddComponent<BossEnding>();
        ending.group = canvas.GetComponent<CanvasGroup>();

        Image black = PresentationUI.NewImage("Black", canvas.transform, new Color(0.01f, 0.015f, 0.025f, 1f));
        PresentationUI.Stretch(black.rectTransform);

        // 가운데 은은한 달빛
        Image glow = PresentationUI.NewImage("Glow", canvas.transform, new Color(0.59f, 0.75f, 1f, 0.10f));
        glow.sprite = BossFxArt.SoftDot;
        RectTransform glowRt = glow.rectTransform;
        glowRt.anchorMin = glowRt.anchorMax = new Vector2(0.5f, 0.54f);
        glowRt.sizeDelta = new Vector2(1300f, 360f);

        RectTransform textRoot = PresentationUI.NewRect("Text", canvas.transform);
        PresentationUI.Stretch(textRoot);
        ending.textGroup = textRoot.gameObject.AddComponent<CanvasGroup>();
        ending.textGroup.alpha = 0f;

        TextMeshProUGUI main = PresentationUI.NewText("Main", textRoot, 72f, new Color(0.93f, 0.95f, 0.98f, 1f), TextAlignmentOptions.Center);
        main.text = MainMessage;
        main.characterSpacing = 12f;
        RectTransform mainRt = main.rectTransform;
        mainRt.anchorMin = mainRt.anchorMax = new Vector2(0.5f, 0.56f);
        mainRt.sizeDelta = new Vector2(1600f, 110f);

        Image line = PresentationUI.NewImage("Line", textRoot, new Color(0.86f, 0.91f, 1f, 0.7f));
        line.sprite = BossFxArt.FadeLine;
        RectTransform lineRt = line.rectTransform;
        lineRt.anchorMin = lineRt.anchorMax = new Vector2(0.5f, 0.49f);
        lineRt.sizeDelta = new Vector2(900f, 2f);

        TextMeshProUGUI sub = PresentationUI.NewText("Sub", textRoot, 28f, new Color(0.80f, 0.85f, 0.94f, 0.8f), TextAlignmentOptions.Center);
        sub.text = SubMessage;
        sub.characterSpacing = 20f;
        RectTransform subRt = sub.rectTransform;
        subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 0.45f);
        subRt.sizeDelta = new Vector2(1400f, 44f);

        RectTransform hintRoot = PresentationUI.NewRect("Hint", canvas.transform);
        hintRoot.anchorMin = hintRoot.anchorMax = new Vector2(0.5f, 0.12f);
        hintRoot.sizeDelta = new Vector2(1400f, 40f);
        ending.hintGroup = hintRoot.gameObject.AddComponent<CanvasGroup>();
        ending.hintGroup.alpha = 0f;
        TextMeshProUGUI hint = PresentationUI.NewText("HintText", hintRoot, 24f, PresentationUI.TextDim, TextAlignmentOptions.Center);
        hint.text = HintMessage;
        PresentationUI.Stretch(hint.rectTransform);

        return ending;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
