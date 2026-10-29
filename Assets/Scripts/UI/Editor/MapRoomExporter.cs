using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Tools/UI > 지도 방 그림 뽑기 (빌드 씬 전체)
///
/// 빌드 목록의 씬(타이틀 제외)을 하나씩 열어서:
///   1. 벽·땅 타일맵(충돌이 있고 한쪽 발판이 아닌 것)을 "막힌 칸"으로 본다. 발판은 무시한다.
///   2. 스폰 위치·포털·제단에서 시작해 막히지 않은 칸을 채워 나간다 → 플레이어가 다닐 수 있는 굴 모양.
///   3. 그 모양을 손으로 판 땅굴처럼 울퉁불퉁하게, 천장에 가끔 종유석을 달아 그림으로 굽는다.
///   4. 포털이 어느 씬의 어느 스폰으로 이어지는지 보고, 방들을 옆에 이어 붙인다.
/// 결과는 Assets/Sprites/UI/MapRooms 그림과 Assets/Resources/MapRoomAtlas 에셋.
/// 끝나면 원래 열려 있던 씬으로 돌아온다. 씬 파일은 바꾸지 않는다.
/// </summary>
public static class MapRoomExporter
{
    private const string OutputFolder = "Assets/Sprites/UI/MapRooms";
    private const string AtlasPath = "Assets/Resources/" + MapRoomAtlas.ResourceName + ".asset";

    private const int PixelsPerCell = 6;      // 타일 한 칸 = 6px
    private const int MaxSide = 4096;
    private const int Pad = 3;                // 방 둘레 여백 (칸)
    private const float DoorGap = 3f;         // 방과 방 사이 (월드 단위)

    private static readonly Color Fill = new Color(0.17f, 0.17f, 0.17f, 1f);

    private class Portal
    {
        public Vector2 Position;
        public string TargetScene;
        public string TargetSpawn;
    }

    private class SceneRoom
    {
        public string Name;
        public byte[] Png;     // 그림은 바로 PNG로 바꿔 둔다. 씬을 바꾸면 메모리의 텍스처가 지워지기 때문.
        public Vector2 WorldMin;
        public Vector2 WorldSize;
        public Rect Solid;     // 막힌 칸의 범위 (월드)
        public Vector2 AtlasMin;
        public bool Placed;
        public readonly List<Portal> Portals = new List<Portal>();
        public readonly Dictionary<string, Vector2> Spawns = new Dictionary<string, Vector2>();
        public Vector2 DefaultSpawn;
        public bool HasDefaultSpawn;
    }

    [MenuItem("Tools/UI/지도 방 그림 뽑기 (빌드 씬 전체)")]
    public static void ExportAll()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("지도 방 그림", "Play를 끄고 다시 눌러 주세요.", "확인");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        List<SceneRoom> rooms = new List<SceneRoom>();

        try
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (!scenes[i].enabled) continue;
                string name = Path.GetFileNameWithoutExtension(scenes[i].path);
                if (name == "Title") continue;

                EditorUtility.DisplayProgressBar("지도 방 그림 뽑기", name, (float)i / scenes.Length);
                Scene scene = EditorSceneManager.OpenScene(scenes[i].path, OpenSceneMode.Single);
                SceneRoom room = Capture(scene, name);
                if (room != null) rooms.Add(room);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (setup != null && setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        if (rooms.Count == 0)
        {
            EditorUtility.DisplayDialog("지도 방 그림", "타일맵이 있는 씬을 찾지 못했어요.", "확인");
            return;
        }

        Layout(rooms);
        Save(rooms);

        EditorUtility.DisplayDialog("지도 방 그림",
            $"방 {rooms.Count}개를 뽑았어요.\n{OutputFolder}\n{AtlasPath}", "확인");
    }

    // ── 씬 하나 → 방 하나 ─────────────────────

    private static SceneRoom Capture(Scene scene, string name)
    {
        // 1. 막힌 칸
        HashSet<Vector2Int> solid = new HashSet<Vector2Int>();
        List<Tilemap> all = new List<Tilemap>();
        foreach (Tilemap t in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            if (t.gameObject.scene == scene && t.gameObject.activeInHierarchy) all.Add(t);

        foreach (Tilemap t in all)
            if (IsSolid(t)) AddCells(t, solid);

        // 벽으로 볼 타일맵이 하나도 없으면 충돌 있는 타일맵을 다 쓴다.
        if (solid.Count == 0)
            foreach (Tilemap t in all)
                if (t.GetComponent<TilemapCollider2D>() != null) AddCells(t, solid);

        if (solid.Count == 0) return null;

        Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
        Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);
        foreach (Vector2Int c in solid)
        {
            min = Vector2Int.Min(min, c);
            max = Vector2Int.Max(max, c);
        }

        SceneRoom room = new SceneRoom { Name = name };
        room.Solid = new Rect(min.x, min.y, max.x - min.x + 1, max.y - min.y + 1);

        // 2. 시작점: 스폰·포털·제단·플레이어
        List<Vector2> seeds = new List<Vector2>();
        foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb == null || mb.gameObject.scene != scene) continue;
            string type = mb.GetType().Name;
            Vector2 pos = mb.transform.position;

            switch (type)
            {
                case "SceneSpawnPoint":
                    seeds.Add(pos);
                    string id = new SerializedObject(mb).FindProperty("spawnId")?.stringValue;
                    if (!string.IsNullOrEmpty(id)) room.Spawns[id] = pos;
                    break;
                case "ScenePortal":
                case "AutoScenePortal":
                    seeds.Add(pos);
                    SerializedObject so = new SerializedObject(mb);
                    string target = so.FindProperty("targetSceneName")?.stringValue;
                    if (!string.IsNullOrEmpty(target))
                    {
                        room.Portals.Add(new Portal
                        {
                            Position = pos,
                            TargetScene = target,
                            TargetSpawn = so.FindProperty("targetSpawnId")?.stringValue
                        });
                    }
                    break;
                case "SceneSpawnManager":
                    Object def = new SerializedObject(mb).FindProperty("defaultSpawnPoint")?.objectReferenceValue;
                    if (def is Transform tr && tr.gameObject.scene == scene)
                    {
                        seeds.Add(tr.position);
                        room.DefaultSpawn = tr.position;
                        room.HasDefaultSpawn = true;
                    }
                    break;
                case "Shrine":
                case "Player":
                    seeds.Add(pos);
                    break;
            }
        }

        // 3. 다닐 수 있는 칸 채우기
        int w = max.x - min.x + 1 + Pad * 2;
        int h = max.y - min.y + 1 + Pad * 2;
        Vector2Int origin = new Vector2Int(min.x - Pad, min.y - Pad);
        bool[,] blocked = new bool[w, h];
        foreach (Vector2Int c in solid) blocked[c.x - origin.x, c.y - origin.y] = true;

        bool[,] inside = Flood(blocked, seeds, origin, w, h);

        // 시작점이 하나도 안 맞으면 범위 안의 빈 칸을 다 쓴다.
        if (!Any(inside))
            for (int x = Pad; x < w - Pad; x++)
            for (int y = Pad; y < h - Pad; y++)
                inside[x, y] = !blocked[x, y];

        room.WorldMin = origin;
        room.WorldSize = new Vector2(w, h);
        Texture2D tex = Bake(inside, blocked, w, h, name.GetHashCode());
        room.Png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        return room;
    }

    private static bool IsSolid(Tilemap t)
    {
        TilemapCollider2D col = t.GetComponent<TilemapCollider2D>();
        if (col == null || !col.enabled || col.isTrigger) return false;
        if (t.GetComponent<PlatformEffector2D>() != null) return false;
        if (col.usedByEffector) return false;
        string n = t.name.ToLowerInvariant();
        if (n.Contains("platform") || n.Contains("oneway")) return false;
        return true;
    }

    private static void AddCells(Tilemap t, HashSet<Vector2Int> cells)
    {
        foreach (Vector3Int p in t.cellBounds.allPositionsWithin)
        {
            if (!t.HasTile(p)) continue;
            Vector3 world = t.GetCellCenterWorld(p);
            cells.Add(new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y)));
        }
    }

    private static bool[,] Flood(bool[,] blocked, List<Vector2> seeds, Vector2Int origin, int w, int h)
    {
        bool[,] inside = new bool[w, h];
        Queue<Vector2Int> q = new Queue<Vector2Int>();

        foreach (Vector2 s in seeds)
        {
            Vector2Int c = new Vector2Int(Mathf.FloorToInt(s.x) - origin.x, Mathf.FloorToInt(s.y) - origin.y);
            // 시작점이 벽에 살짝 묻혀 있으면 위로 몇 칸 올려 본다.
            for (int up = 0; up < 4; up++)
            {
                Vector2Int t = c + Vector2Int.up * up;
                if (t.x < 0 || t.y < 0 || t.x >= w || t.y >= h || blocked[t.x, t.y]) continue;
                if (!inside[t.x, t.y])
                {
                    inside[t.x, t.y] = true;
                    q.Enqueue(t);
                }
                break;
            }
        }

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (q.Count > 0)
        {
            Vector2Int c = q.Dequeue();
            foreach (Vector2Int d in dirs)
            {
                Vector2Int n = c + d;
                // 바깥 여백(Pad)까지는 가지 않는다. 뚫린 하늘로 새어 나가 판 전체가 칠해지지 않게.
                if (n.x < Pad || n.y < Pad || n.x >= w - Pad || n.y >= h - Pad) continue;
                if (blocked[n.x, n.y] || inside[n.x, n.y]) continue;
                inside[n.x, n.y] = true;
                q.Enqueue(n);
            }
        }

        return inside;
    }

    private static bool Any(bool[,] a)
    {
        foreach (bool b in a) if (b) return true;
        return false;
    }

    // ── 땅굴 그림 굽기 ─────────────────────────

    private static Texture2D Bake(bool[,] inside, bool[,] blocked, int w, int h, int seed)
    {
        int ppc = Mathf.Clamp(Mathf.Min(PixelsPerCell, MaxSide / Mathf.Max(w, h)), 2, PixelsPerCell);
        int pw = w * ppc, ph = h * ppc;
        System.Random rnd = new System.Random(seed);

        // 테두리를 흔드는 부드러운 잡음 (3칸 간격 격자)
        int gw = w / 3 + 3, gh = h / 3 + 3;
        float[,] nx = new float[gw, gh], ny = new float[gw, gh];
        for (int x = 0; x < gw; x++)
        for (int y = 0; y < gh; y++)
        {
            nx[x, y] = (float)(rnd.NextDouble() - 0.5) * 0.7f;
            ny[x, y] = (float)(rnd.NextDouble() - 0.5) * 0.7f;
        }

        // 천장 종유석, 바닥 돌무더기
        Dictionary<Vector2Int, Vector3> spikes = new Dictionary<Vector2Int, Vector3>();
        Dictionary<Vector2Int, Vector2> bumps = new Dictionary<Vector2Int, Vector2>();
        for (int x = 1; x < w - 1; x++)
        for (int y = 1; y < h - 1; y++)
        {
            if (!inside[x, y]) continue;
            if (blocked[x, y + 1] && rnd.NextDouble() < 0.14)
                spikes[new Vector2Int(x, y)] = new Vector3(
                    x + 0.25f + (float)rnd.NextDouble() * 0.5f,      // 끝 x
                    0.55f + (float)rnd.NextDouble() * 0.6f,          // 길이
                    0.22f + (float)rnd.NextDouble() * 0.12f);        // 반폭
            if (blocked[x, y - 1] && rnd.NextDouble() < 0.07)
                bumps[new Vector2Int(x, y)] = new Vector2(x + 0.5f, 0.25f + (float)rnd.NextDouble() * 0.2f);
        }

        bool[] mask = new bool[pw * ph];
        for (int py = 0; py < ph; py++)
        for (int px = 0; px < pw; px++)
        {
            float fx = (px + 0.5f) / ppc, fy = (py + 0.5f) / ppc;
            fx += Sample(nx, fx / 3f, fy / 3f);
            fy += Sample(ny, fx / 3f, fy / 3f);

            int cx = Mathf.FloorToInt(fx), cy = Mathf.FloorToInt(fy);
            if (cx < 0 || cy < 0 || cx >= w || cy >= h || !inside[cx, cy]) continue;

            Vector2Int c = new Vector2Int(cx, cy);
            if (spikes.TryGetValue(c, out Vector3 s))
            {
                float top = cy + 1f, tip = top - s.y;
                if (fy > tip && Mathf.Abs(fx - s.x) < s.z * (fy - tip) / s.y) continue;
            }
            if (bumps.TryGetValue(c, out Vector2 b))
            {
                float d = Mathf.Abs(fx - b.x);
                if (fy - cy < b.y * Mathf.Clamp01(1f - d * 1.6f)) continue;
            }

            mask[py * pw + px] = true;
        }

        Color[] pixels = new Color[pw * ph];
        const int line = 2, glow = 5;
        for (int py = 0; py < ph; py++)
        for (int px = 0; px < pw; px++)
        {
            bool inn = mask[py * pw + px];
            int nearest = Nearest(mask, pw, ph, px, py, !inn, inn ? line : glow);

            if (inn)
                pixels[py * pw + px] = nearest <= line ? Color.white : Fill;
            else if (nearest <= glow)
                pixels[py * pw + px] = new Color(1f, 1f, 1f, 0.13f * (1f - (nearest - 1f) / glow));
            else
                pixels[py * pw + px] = Color.clear;
        }

        Texture2D tex = new Texture2D(pw, ph, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>가장 가까운 (want) 픽셀까지 거리. range 안에 없으면 int.MaxValue.</summary>
    private static int Nearest(bool[] mask, int pw, int ph, int px, int py, bool want, int range)
    {
        for (int r = 1; r <= range; r++)
        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
        {
            if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
            int x = px + dx, y = py + dy;
            bool v = x >= 0 && y >= 0 && x < pw && y < ph && mask[y * pw + x];
            if (v == want) return r;
        }
        return int.MaxValue;
    }

    private static float Sample(float[,] g, float x, float y)
    {
        int gw = g.GetLength(0), gh = g.GetLength(1);
        x = Mathf.Clamp(x, 0, gw - 1.001f);
        y = Mathf.Clamp(y, 0, gh - 1.001f);
        int x0 = (int)x, y0 = (int)y;
        float tx = x - x0, ty = y - y0;
        tx = tx * tx * (3 - 2 * tx);
        ty = ty * ty * (3 - 2 * ty);
        float a = Mathf.Lerp(g[x0, y0], g[x0 + 1, y0], tx);
        float b = Mathf.Lerp(g[x0, y0 + 1], g[x0 + 1, y0 + 1], tx);
        return Mathf.Lerp(a, b, ty);
    }

    // ── 방 이어 붙이기 ─────────────────────────

    private static void Layout(List<SceneRoom> rooms)
    {
        Dictionary<string, SceneRoom> byName = new Dictionary<string, SceneRoom>();
        foreach (SceneRoom r in rooms) byName[r.Name] = r;

        float nextFreeX = 0f;

        foreach (SceneRoom start in rooms)
        {
            if (start.Placed) continue;

            // 이어지지 않은 덩어리는 오른쪽에 띄워서 놓는다.
            start.AtlasMin = new Vector2(nextFreeX, 0f);
            start.Placed = true;

            Queue<SceneRoom> q = new Queue<SceneRoom>();
            q.Enqueue(start);

            while (q.Count > 0)
            {
                SceneRoom a = q.Dequeue();

                // a → b (a의 포털이 b로)
                foreach (Portal p in a.Portals)
                {
                    if (!byName.TryGetValue(p.TargetScene, out SceneRoom b) || b.Placed) continue;
                    Vector2 arrive = ArrivalIn(b, p.TargetSpawn, a.Name);
                    Vector2 dir = Side(a, p.Position);
                    b.AtlasMin = MapRoomAtlas.ToAtlas(Temp(a), p.Position) + dir * DoorGap - (arrive - b.WorldMin);
                    b.Placed = true;
                    q.Enqueue(b);
                }

                // b → a (b의 포털이 a로)
                foreach (SceneRoom b in rooms)
                {
                    if (b.Placed) continue;
                    foreach (Portal p in b.Portals)
                    {
                        if (p.TargetScene != a.Name) continue;
                        Vector2 arrive = ArrivalIn(a, p.TargetSpawn, b.Name);
                        Vector2 dir = Side(b, p.Position);
                        // b의 포털이 a의 도착 지점 바로 옆에 오게
                        Vector2 atlasArrive = MapRoomAtlas.ToAtlas(Temp(a), arrive);
                        b.AtlasMin = atlasArrive - dir * DoorGap - (p.Position - b.WorldMin);
                        b.Placed = true;
                        q.Enqueue(b);
                        break;
                    }
                }
            }

            foreach (SceneRoom r in rooms)
                if (r.Placed) nextFreeX = Mathf.Max(nextFreeX, r.AtlasMin.x + r.WorldSize.x + 20f);
        }
    }

    private static MapRoomAtlas.Room Temp(SceneRoom r) =>
        new MapRoomAtlas.Room { worldMin = r.WorldMin, atlasMin = r.AtlasMin, worldSize = r.WorldSize };

    /// <summary>b 씬에 들어왔을 때 서는 곳. 스폰 id → b에서 from으로 가는 포털 → 기본 스폰 → 가운데.</summary>
    private static Vector2 ArrivalIn(SceneRoom b, string spawnId, string from)
    {
        if (!string.IsNullOrEmpty(spawnId) && b.Spawns.TryGetValue(spawnId, out Vector2 s)) return s;
        foreach (Portal p in b.Portals) if (p.TargetScene == from) return p.Position;
        if (b.HasDefaultSpawn) return b.DefaultSpawn;
        return b.Solid.center;
    }

    /// <summary>포털이 방의 어느 쪽 끝에 붙어 있는지. 그쪽으로 다음 방을 붙인다.</summary>
    private static Vector2 Side(SceneRoom r, Vector2 p)
    {
        Rect s = r.Solid;
        float left = p.x - s.xMin, right = s.xMax - p.x, down = p.y - s.yMin, up = s.yMax - p.y;
        float m = Mathf.Min(Mathf.Min(left, right), Mathf.Min(down, up));
        if (m == left) return Vector2.left;
        if (m == right) return Vector2.right;
        if (m == down) return Vector2.down;
        return Vector2.up;
    }

    // ── 저장 ──────────────────────────────

    private static void Save(List<SceneRoom> rooms)
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
            AssetDatabase.Refresh();
        }

        List<MapRoomAtlas.Room> entries = new List<MapRoomAtlas.Room>();

        foreach (SceneRoom r in rooms)
        {
            string path = $"{OutputFolder}/{r.Name}_room.png";
            File.WriteAllBytes(path, r.Png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = MaxSide;
            ti.SaveAndReimport();

            entries.Add(new MapRoomAtlas.Room
            {
                sceneName = r.Name,
                image = AssetDatabase.LoadAssetAtPath<Sprite>(path),
                worldMin = r.WorldMin,
                worldSize = r.WorldSize,
                atlasMin = r.AtlasMin
            });
        }

        MapRoomAtlas atlas = AssetDatabase.LoadAssetAtPath<MapRoomAtlas>(AtlasPath);
        if (atlas == null)
        {
            atlas = ScriptableObject.CreateInstance<MapRoomAtlas>();
            AssetDatabase.CreateAsset(atlas, AtlasPath);
        }

        atlas.SetRooms(entries);
        EditorUtility.SetDirty(atlas);
        AssetDatabase.SaveAssets();
    }
}
