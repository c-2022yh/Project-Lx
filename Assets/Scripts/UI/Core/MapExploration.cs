using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 지도 안개용 탐험 기록. 플레이어 주변을 칸 단위로 밝혀서 RunState에 남긴다.
///
/// 월드를 CellSize 크기 칸으로 나누고, 플레이어 반경 RevealRadius 안의 칸을 "밝힘"으로 기록한다.
/// 지도(MapPanel)는 열 때 이 기록을 읽어 밝힌 칸만 안개를 걷어낸다.
///
/// 씬에도 프리팹에도 붙이지 않는다. 게임이 시작되면 스스로 생겨 DontDestroyOnLoad로 산다.
/// 지금은 메모리 기록이라 게임을 끄면 사라진다. 세이브가 생기면 RunState째로 저장된다.
/// </summary>
public class MapExploration : MonoBehaviour
{
    /// <summary>한 칸의 크기 (월드 단위). 타일 한 칸 = 1이다.</summary>
    public const float CellSize = 2f;

    /// <summary>플레이어 주변 이 반경 안이 밝혀진다. 대략 카메라 화면 높이 정도.</summary>
    public const float RevealRadius = 9f;

    private const float Interval = 0.2f;

    private static MapExploration instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        GameObject go = new GameObject("[MapExploration]");
        instance = go.AddComponent<MapExploration>();
        DontDestroyOnLoad(go);
    }

    private Transform player;
    private float nextAt;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextAt) return;
        nextAt = Time.unscaledTime + Interval;

        if (player == null)
        {
            Player found = FindAnyObjectByType<Player>();
            if (found == null) return;   // 타이틀처럼 플레이어가 없는 씬

            player = found.transform;
        }

        Reveal(SceneManager.GetActiveScene().name, player.position);
    }

    public static long CellKey(int x, int y) => ((long)x << 32) | (uint)y;

    public static long CellOf(Vector2 world) =>
        CellKey(Mathf.FloorToInt(world.x / CellSize), Mathf.FloorToInt(world.y / CellSize));

    public static bool IsExplored(string sceneId, Vector2 world) =>
        RunState.Current.IsExplored(sceneId, CellOf(world));

    /// <summary>center 주변 반경 안의 칸을 밝힌다.</summary>
    public static void Reveal(string sceneId, Vector2 center)
    {
        int cx = Mathf.FloorToInt(center.x / CellSize);
        int cy = Mathf.FloorToInt(center.y / CellSize);
        int range = Mathf.CeilToInt(RevealRadius / CellSize);
        float radiusSq = RevealRadius * RevealRadius;

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                // 칸 가운데가 반경 안에 들어오면 밝힌다.
                float wx = (cx + dx + 0.5f) * CellSize - center.x;
                float wy = (cy + dy + 0.5f) * CellSize - center.y;
                if (wx * wx + wy * wy > radiusSq) continue;

                RunState.Current.MarkExplored(sceneId, CellKey(cx + dx, cy + dy));
            }
        }
    }

    // ── 에디터 테스트용 끄기 ─────────────────

    /// <summary>false면 지도가 안개 없이 전부 보인다.</summary>
    public static bool FogEnabled => !IsDisabledInEditor;

#if UNITY_EDITOR
    private const string DisableKey = "Ludens.UI.DisableMapFog";
    private const string DisableMenu = "Tools/UI/지도 안개 끄기 (테스트용)";

    private static bool IsDisabledInEditor => EditorPrefs.GetBool(DisableKey, false);

    [MenuItem(DisableMenu)]
    private static void ToggleDisabled()
    {
        bool next = !IsDisabledInEditor;
        EditorPrefs.SetBool(DisableKey, next);
        Debug.Log(next ? "[MapExploration] 지도 안개: 끔" : "[MapExploration] 지도 안개: 켬");
    }

    [MenuItem(DisableMenu, true)]
    private static bool ToggleDisabledValidate()
    {
        Menu.SetChecked(DisableMenu, IsDisabledInEditor);
        return true;
    }
#else
    private static bool IsDisabledInEditor => false;
#endif
}
