using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 지도에 "한 번 쓰러뜨린 정예 몬스터·보스" 자리를 남긴다 (11.01).
///
/// 씬의 정예(EnemyEliteAI)·보스(BossMarker, GolemBossController)를 지켜보다가 쓰러지면
/// 그 자리를 RunState 플래그로 기억한다: "map.defeated|EliteEnemy|씬|x|y".
/// 지도(MapPanel)는 열 때 지금 씬의 기록을 읽어 마커로 찍는다.
/// 적 코드는 고치지 않는다 (죽었는지만 읽는다).
/// 씬에도 프리팹에도 붙이지 않는다. 게임이 시작되면 스스로 생겨 DontDestroyOnLoad로 산다.
/// </summary>
public class MapDefeatedMarks : MonoBehaviour
{
    private const string Prefix = "map.defeated|";
    private const float TickInterval = 0.25f;
    private const float ScanInterval = 1f;
    private const float SameSpotDistance = 3f;   // 이 거리 안에 같은 기록이 있으면 또 남기지 않는다

    private static MapDefeatedMarks instance;

    private readonly Dictionary<int, (Enemy enemy, MapMarkerKind kind)> watching = new Dictionary<int, (Enemy, MapMarkerKind)>();
    private readonly Dictionary<int, GolemBossController> golems = new Dictionary<int, GolemBossController>();
    private float nextTick, nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("[MapDefeatedMarks]");
        instance = go.AddComponent<MapDefeatedMarks>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬이 바뀌며 사라진 적을 "쓰러뜨림"으로 잘못 기록하지 않게, 지켜보던 목록을 비운다
    private void OnSceneUnloaded(Scene scene) { watching.Clear(); golems.Clear(); }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { nextScan = 0f; }

    private void Update()
    {
        float now = Time.unscaledTime;
        if (now >= nextScan)
        {
            nextScan = now + ScanInterval;
            Scan();
        }
        if (now < nextTick) return;
        nextTick = now + TickInterval;
        Check();
    }

    private void Scan()
    {
        foreach (Enemy e in FindObjectsByType<Enemy>())
        {
            if (e == null || e.IsDead) continue;
            int id = e.GetInstanceID();
            if (watching.ContainsKey(id)) continue;
            if (e.GetComponent<BossMarker>() != null) watching[id] = (e, MapMarkerKind.Boss);
            else if (e.GetComponent<EnemyEliteAI>() != null) watching[id] = (e, MapMarkerKind.EliteEnemy);
        }
        foreach (GolemBossController g in FindObjectsByType<GolemBossController>())
            if (g != null && !g.IsDefeated) golems[g.GetInstanceID()] = g;
    }

    private void Check()
    {
        if (watching.Count > 0)
        {
            List<int> done = null;
            foreach (var pair in watching)
            {
                Enemy e = pair.Value.enemy;
                if (e == null) { (done ??= new List<int>()).Add(pair.Key); continue; }
                if (!e.IsDead) continue;
                Record(pair.Value.kind, e.transform.position);
                (done ??= new List<int>()).Add(pair.Key);
            }
            if (done != null) foreach (int id in done) watching.Remove(id);
        }

        if (golems.Count > 0)
        {
            List<int> done = null;
            foreach (var pair in golems)
            {
                GolemBossController g = pair.Value;
                if (g == null) { (done ??= new List<int>()).Add(pair.Key); continue; }
                if (!g.IsDefeated) continue;
                Record(MapMarkerKind.Boss, g.transform.position);
                (done ??= new List<int>()).Add(pair.Key);
            }
            if (done != null) foreach (int id in done) golems.Remove(id);
        }
    }

    private static void Record(MapMarkerKind kind, Vector2 position)
    {
        string scene = SceneManager.GetActiveScene().name;
        foreach (var mark in InScene(scene))
            if (mark.kind == kind && Vector2.Distance(mark.position, position) < SameSpotDistance) return;

        string flag = Prefix + kind + "|" + scene + "|"
                      + position.x.ToString("0.0", CultureInfo.InvariantCulture) + "|"
                      + position.y.ToString("0.0", CultureInfo.InvariantCulture);
        RunState.Current.SetFlag(flag);
    }

    /// <summary>이 씬에서 쓰러뜨린 정예·보스 자리.</summary>
    public static List<(MapMarkerKind kind, Vector2 position)> InScene(string scene)
    {
        List<(MapMarkerKind, Vector2)> result = new List<(MapMarkerKind, Vector2)>();
        foreach (string flag in RunState.Current.Flags)
        {
            if (flag == null || !flag.StartsWith(Prefix)) continue;
            string[] p = flag.Substring(Prefix.Length).Split('|');
            if (p.Length != 4 || p[1] != scene) continue;
            if (!System.Enum.TryParse(p[0], out MapMarkerKind kind)) continue;
            if (!float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) continue;
            if (!float.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) continue;
            result.Add((kind, new Vector2(x, y)));
        }
        return result;
    }
}
