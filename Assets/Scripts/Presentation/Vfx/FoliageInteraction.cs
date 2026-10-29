using UnityEngine;

/// <summary>
/// 풀·덤불 흔들림 셰이더(ProjectLx/2D/FoliageSway)에 플레이어 위치와 속도를 알려준다.
/// 셰이더는 이 값으로 플레이어 근처의 풀을 옆으로 밀고, 걸어갈 때 바스락 흔든다.
///
/// 씬에도 프리팹에도 붙이지 않는다. 게임이 시작되면 스스로 생긴다.
/// </summary>
public class FoliageInteraction : MonoBehaviour
{
    private static readonly int PlayerId = Shader.PropertyToID("_FoliagePlayer");

    private const float FullSpeed = 6f;          // 이 속도면 바스락 최대
    private const float FootOffsetY = 0.4f;      // 발밑보다 조금 위를 기준으로

    private static FoliageInteraction instance;

    private Player player;
    private Rigidbody2D rb;
    private float nextSearchTime;
    private float smoothedSpeed;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("[FoliageInteraction]");
        instance = go.AddComponent<FoliageInteraction>();
        DontDestroyOnLoad(go);
        Shader.SetGlobalVector(PlayerId, Vector4.zero);
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            Shader.SetGlobalVector(PlayerId, Vector4.zero);   // w=0: 플레이어 없음
            if (Time.unscaledTime < nextSearchTime) return;
            nextSearchTime = Time.unscaledTime + 0.5f;
            player = FindFirstObjectByType<Player>();
            rb = player != null ? player.GetComponent<Rigidbody2D>() : null;
            if (player == null) return;
        }

        float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, Mathf.Clamp01(speed / FullSpeed), 1f - Mathf.Exp(-10f * Time.deltaTime));

        Vector3 p = player.transform.position;
        Shader.SetGlobalVector(PlayerId, new Vector4(p.x, p.y + FootOffsetY, smoothedSpeed, 1f));
    }
}
