using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 화면 흔들림. 설정창 "카메라 흔들림"을 끄면 아무것도 하지 않는다.
///
///   CameraShake.Shake(CameraShake.Light);            // 전체적으로 흔들기 (0~1, 겹치면 더해짐)
///   CameraShake.Kick(Vector2.right, 0.1f);           // 한 방향으로 툭 밀었다 돌아오기 (타격감)
///
/// 흔들림 양은 trauma² 에 비례한다 (작은 값은 아주 약하게, 큰 값은 확 세게).
/// 시간은 실제 시간으로 흘러서 히트스탑 중에도 흔들린다.
///
/// 씬의 CinemachineCamera마다 CameraShakeExtension을 자동으로 붙인다. 씬/프리팹은 건드리지 않는다.
/// 런타임에 새로 만든 카메라는 CameraShake.Attach(cam)을 불러 준다.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public const float Light = 0.18f;
    public const float Medium = 0.35f;
    public const float Heavy = 0.6f;

    private const float MaxOffset = 0.45f;     // trauma 1일 때 최대 이동 거리(월드 단위)
    private const float Frequency = 22f;       // 흔들림 속도
    private const float TraumaDecay = 1.6f;    // 초당 줄어드는 양
    private const float KickReturn = 18f;      // 킥이 돌아오는 속도

    private static CameraShake instance;

    private float trauma;
    private Vector2 kick;
    private float seed;
    private float time;

    /// <summary>CameraShakeExtension이 매 프레임 읽어 간다.</summary>
    public static Vector3 CurrentOffset { get; private set; }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        CurrentOffset = Vector3.zero;
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Ensure();
        AttachAll();
    }

    public static void Shake(float amount)
    {
        if (!Enabled || amount <= 0f) return;
        CameraShake s = Ensure();
        s.trauma = Mathf.Clamp01(s.trauma + amount);
    }

    public static void Kick(Vector2 direction, float distance)
    {
        if (!Enabled || distance <= 0f || direction == Vector2.zero) return;
        CameraShake s = Ensure();
        s.kick += direction.normalized * distance;
        s.kick = Vector2.ClampMagnitude(s.kick, MaxOffset);
    }

    public static void Stop()
    {
        if (instance == null) return;
        instance.trauma = 0f;
        instance.kick = Vector2.zero;
        CurrentOffset = Vector3.zero;
    }

    /// <summary>런타임에 만든 카메라(보스방 카메라 등)에 흔들림을 붙인다.</summary>
    public static void Attach(CinemachineCamera cam)
    {
        if (cam == null || cam.GetComponent<CameraShakeExtension>() != null) return;
        cam.gameObject.AddComponent<CameraShakeExtension>();
    }

    private static bool Enabled => GameSettings.CameraShake;

    private static CameraShake Ensure()
    {
        if (instance != null) return instance;
        GameObject go = new GameObject("[CameraShake]");
        instance = go.AddComponent<CameraShake>();
        DontDestroyOnLoad(go);
        return instance;
    }

    private static void AttachAll()
    {
        foreach (CinemachineCamera cam in FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Attach(cam);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        seed = Random.value * 100f;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Stop();
        AttachAll();
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        time += dt;

        if (!Enabled)
        {
            trauma = 0f;
            kick = Vector2.zero;
            CurrentOffset = Vector3.zero;
            return;
        }

        trauma = Mathf.Max(0f, trauma - TraumaDecay * dt);
        kick = Vector2.Lerp(kick, Vector2.zero, 1f - Mathf.Exp(-KickReturn * dt));

        float power = trauma * trauma;
        float x = (Mathf.PerlinNoise(seed, time * Frequency) * 2f - 1f) * MaxOffset * power;
        float y = (Mathf.PerlinNoise(seed + 37.1f, time * Frequency) * 2f - 1f) * MaxOffset * power;

        CurrentOffset = new Vector3(x + kick.x, y + kick.y, 0f);
    }
}
