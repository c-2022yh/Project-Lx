using UnityEngine;

/// <summary>
/// 히트스탑(타격 순간 시간 멈춤)과 슬로모션.
///
///   HitStop.Stop(0.05f);          // 0.05초 동안 거의 멈춤
///   HitStop.Slow(0.3f, 1f);       // 1초 동안 0.3배속
///
/// 시간은 실제 시간(unscaled)으로 잰다. 여러 요청이 겹치면 더 느린 쪽, 더 늦게 끝나는 쪽을 따른다.
/// 일시정지(timeScale 0)나 다른 코드가 timeScale을 바꾸면 그쪽을 존중하고 조용히 빠진다.
///
/// 씬에도 프리팹에도 붙이지 않는다. 처음 부를 때 스스로 생긴다.
/// </summary>
public class HitStop : MonoBehaviour
{
    /// <summary>완전히 0으로 하면 일시정지와 구분이 안 돼서 아주 작은 값을 쓴다.</summary>
    public const float StopScale = 0.001f;

    /// <summary>연타할 때 멈춤이 이어 붙어 끈적해지지 않도록 쉬는 간격.</summary>
    private const float MinGapBetweenStops = 0.04f;

    private static HitStop instance;

    private bool active;
    private float currentScale = 1f;
    private float restoreScale = 1f;
    private float endTime;
    private float lastEndTime = -10f;

    public static bool IsActive => instance != null && instance.active;

    /// <summary>히트스탑이 끝나면 돌아갈 timeScale. 다른 UI가 시간을 멈출 때 저장용으로 쓴다.</summary>
    public static float RestoreScale => IsActive ? instance.restoreScale : Time.timeScale;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;
#endif

    public static void Stop(float seconds) => Request(StopScale, seconds, true);

    public static void Slow(float scale, float seconds) => Request(Mathf.Clamp(scale, StopScale, 1f), seconds, false);

    /// <summary>진행 중인 히트스탑을 바로 끝낸다.</summary>
    public static void Cancel()
    {
        if (instance == null || !instance.active) return;
        instance.Finish(true);
    }

    private static void Request(float scale, float seconds, bool isStop)
    {
        if (seconds <= 0f) return;
        if (Time.timeScale <= 0f) return;   // 일시정지 중

        HitStop s = Ensure();
        float now = Time.unscaledTime;

        if (!s.active)
        {
            if (isStop && now - s.lastEndTime < MinGapBetweenStops) return;

            s.active = true;
            s.restoreScale = Time.timeScale;
            s.currentScale = scale;
            s.endTime = now + seconds;
        }
        else
        {
            s.currentScale = Mathf.Min(s.currentScale, scale);
            s.endTime = Mathf.Max(s.endTime, now + seconds);
        }

        Time.timeScale = s.currentScale;
    }

    private static HitStop Ensure()
    {
        if (instance != null) return instance;
        GameObject go = new GameObject("[HitStop]");
        instance = go.AddComponent<HitStop>();
        DontDestroyOnLoad(go);
        return instance;
    }

    private void Update()
    {
        if (!active) return;

        // 일시정지·팝업·씬 이동 등으로 누가 timeScale을 바꿨다면 손대지 않고 빠진다.
        if (!Mathf.Approximately(Time.timeScale, currentScale))
        {
            Finish(false);
            return;
        }

        if (Time.unscaledTime >= endTime) Finish(true);
    }

    private void Finish(bool restore)
    {
        if (restore) Time.timeScale = restoreScale;
        active = false;
        lastEndTime = Time.unscaledTime;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        if (active) Time.timeScale = restoreScale;
        instance = null;
    }
}
