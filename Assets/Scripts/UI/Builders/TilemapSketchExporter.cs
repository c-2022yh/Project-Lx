#if UNITY_EDITOR && LUDENS_UI_TOOLS
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 열려 있는 씬의 타일맵을 한 장의 PNG로 뽑는다.
/// Unity 상단 메뉴 Tools/UI > 맵 밑그림 뽑기.
///
/// 타일 그림을 그대로 찍지 않고, 타일이 있는 칸을 색 블록으로 채운 "실루엣"을 만든다.
/// 텍스처 Read/Write가 꺼져 있어도 되고, 지형의 모양만 보면 되는 밑그림에는 이쪽이 낫다.
///
/// 이걸 디자인 담당에게 밑그림으로 넘기면, 그 위에 그린 맵이 자동으로 좌표가 맞는다.
/// 캔버스 크기와 비율이 같기 때문이다.
///
/// 뽑은 뒤에는 좌표 값까지 MapSketchLibrary(Assets/Resources)에 바로 등록한다.
/// 맵 창은 열릴 때 그 저장소를 씬 이름으로 조회하므로, 인스펙터에 손으로
/// 옮겨 적을 값이 없다. 씬을 하나 열고 이 메뉴를 돌리면 그 씬 지도가 끝난다.
/// </summary>
public static class TilemapSketchExporter
{
    /// <summary>타일 한 칸을 몇 픽셀로 그릴지. 이 값이 그대로 MapPanel의 Units To Pixels가 된다.</summary>
    private const int PixelsPerTile = 8;

    /// <summary>너무 큰 맵에서 메모리를 통째로 먹지 않도록 거는 상한.</summary>
    private const int MaxTextureSide = 8192;

    private const string OutputFolder = "Assets/Sprites/UI/MapSketch";

    private const string LibraryFolder = "Assets/Resources";
    private const string LibraryPath =
        LibraryFolder + "/" + MapSketchLibrary.ResourceName + ".asset";

    /// <summary>테두리 두께(픽셀). 칸 안쪽에 그리므로 이미지 크기는 안 변한다.</summary>
    private const int OutlineThickness = 1;

    // 밑그림은 전부 무채색으로 간다. 색은 마커 몫이다.
    // 지형에 색을 주면 그 위에 찍히는 제단·상자·보스 마커가 묻힌다.
    // 층 구분은 색상이 아니라 명도로만 준다.
    private static readonly Color GroundColor = new Color(0.21f, 0.21f, 0.21f, 1f);
    private static readonly Color PlatformColor = new Color(0.28f, 0.28f, 0.28f, 1f);
    private static readonly Color SpikeColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color SecretColor = new Color(0.16f, 0.16f, 0.16f, 1f);
    private static readonly Color OtherColor = new Color(0.24f, 0.24f, 0.24f, 1f);

    /// <summary>지형 실루엣을 두르는 밝은 선. 면이 어두워서 이 선이 모양을 다 설명한다.</summary>
    private static readonly Color OutlineColor = new Color(0.70f, 0.70f, 0.70f, 1f);

    [MenuItem("Tools/UI/맵 밑그림 뽑기 (열린 씬 타일맵 → PNG)")]
    public static void Export()
    {
        Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

        if (tilemaps.Length == 0)
        {
            EditorUtility.DisplayDialog("타일맵 없음",
                "열려 있는 씬에서 Tilemap을 찾지 못했습니다.\n맵이 있는 씬을 먼저 열어주세요.", "확인");
            return;
        }

        // 타일이 실제로 칠해진 칸만 모아서 전체 범위를 잡는다.
        // cellBounds는 지웠던 자리까지 포함해 부풀어 있는 경우가 많다.
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        List<Tilemap> used = new List<Tilemap>();

        foreach (Tilemap map in tilemaps)
        {
            BoundsInt bounds = map.cellBounds;
            bool any = false;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    if (!map.HasTile(new Vector3Int(x, y, 0))) continue;

                    any = true;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (any) used.Add(map);
        }

        if (used.Count == 0)
        {
            EditorUtility.DisplayDialog("빈 타일맵",
                "타일맵은 있는데 칠해진 타일이 하나도 없습니다.", "확인");
            return;
        }

        int tilesWide = maxX - minX + 1;
        int tilesHigh = maxY - minY + 1;

        int width = tilesWide * PixelsPerTile;
        int height = tilesHigh * PixelsPerTile;

        if (width > MaxTextureSide || height > MaxTextureSide)
        {
            EditorUtility.DisplayDialog("맵이 너무 큽니다",
                $"뽑으면 {width} x {height} 픽셀이 됩니다.\n" +
                $"타일 한 칸당 픽셀 수(PixelsPerTile = {PixelsPerTile})를 줄여주세요.", "확인");
            return;
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0f, 0f, 0f, 0f);

        // 어느 칸이 찼는지 따로 들고 있어야 테두리를 그릴 수 있다.
        // 레이어가 여러 장이라, 한 장씩 보면 옆 레이어와 맞닿은 면까지 테두리가 생긴다.
        bool[] occupied = new bool[tilesWide * tilesHigh];

        foreach (Tilemap map in used)
        {
            Color color = ColorFor(map.gameObject.name);
            BoundsInt bounds = map.cellBounds;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    if (!map.HasTile(new Vector3Int(x, y, 0))) continue;

                    int cellX = x - minX;
                    int cellY = y - minY;

                    occupied[cellY * tilesWide + cellX] = true;
                    FillCell(pixels, width, height, cellX, cellY, color);
                }
            }
        }

        DrawOutline(pixels, width, height, occupied, tilesWide, tilesHigh);

        texture.SetPixels(pixels);
        texture.Apply();

        // Directory.CreateDirectory로 만든 폴더는 AssetDatabase가 모르는 상태라
        // 바로 ImportAsset을 부르면 실패한다. 이쪽 API로 한 단계씩 만든다.
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites/UI"))
            AssetDatabase.CreateFolder("Assets/Sprites", "UI");
        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder("Assets/Sprites/UI", "MapSketch");

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName)) sceneName = "Scene";

        string path = OutputFolder + "/" + sceneName + "_sketch.png";
        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        // 기본값(Default)으로 들어오면 Image에 끼울 수가 없다. Sprite로 바꿔준다.
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerTile;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        AssetDatabase.Refresh();

        // 칸 (x, y)는 월드에서 [x, x+1] 구간을 덮는다. 그래서 오른쪽 끝에 1을 더한다.
        float centerX = (minX + maxX + 1) * 0.5f;
        float centerY = (minY + maxY + 1) * 0.5f;

        Register(sceneName, path, new Vector2(centerX, centerY));

        EditorUtility.DisplayDialog("맵 밑그림 완성",
            path + "\n\n" +
            $"이미지 크기  {width} x {height} px  (타일 {tilesWide} x {tilesHigh})\n" +
            $"타일맵 {used.Count}개를 합쳤습니다.\n\n" +
            $"\"{sceneName}\" 씬 항목으로 등록했습니다.\n" +
            $"    World Center     X {centerX}   Y {centerY}\n" +
            $"    Units To Pixels  {PixelsPerTile}\n\n" +
            "이제 그 씬에서 탭을 누르면 이 지형이 깔립니다.\n" +
            "인스펙터에 옮겨 적을 값은 없습니다.\n\n" +
            "다른 씬 지도는 그 씬을 열고 이 메뉴를 다시 돌리면 됩니다.\n" +
            "디자인 작업물로 갈아끼울 때는 같은 크기 캔버스에 그려서\n" +
            "저장소(Assets/Resources/MapSketchLibrary)의 Sketch만 바꾸면 좌표가 맞습니다.",
            "확인");

        Debug.Log($"[TilemapSketchExporter] {path} / World Center ({centerX}, {centerY}) / Units To Pixels {PixelsPerTile}");
    }

    /// <summary>
    /// 뽑은 밑그림을 씬 이름으로 저장소에 등록한다.
    ///
    /// 맵 창은 프리팹에 하나뿐이라 이미지를 거기 직접 박으면 모든 씬이 같은 지도를
    /// 쓰게 된다. 그래서 프리팹을 건드리지 않고 저장소에만 적어둔다.
    /// 같은 씬을 다시 뽑으면 그 항목만 덮어쓴다.
    /// </summary>
    private static void Register(string sceneName, string spritePath, Vector2 worldCenter)
    {
        MapSketchLibrary library = AssetDatabase.LoadAssetAtPath<MapSketchLibrary>(LibraryPath);

        if (library == null)
        {
            // Directory.CreateDirectory로 만든 폴더는 AssetDatabase가 아직 모른다.
            // 그 상태로 CreateAsset을 부르면 실패하므로 이쪽 API로 만든다.
            if (!AssetDatabase.IsValidFolder(LibraryFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");

            library = ScriptableObject.CreateInstance<MapSketchLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        MapSketchLibrary.Entry entry = library.Upsert(sceneName);

        entry.sketch = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        entry.worldCenter = worldCenter;
        entry.unitsToPixels = PixelsPerTile;

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 지형이 바깥 공기와 맞닿는 면에만 밝은 선을 긋는다.
    ///
    /// 면 색을 다 죽여놔서, 이 선이 없으면 어두운 판 위에서 모양이 안 읽힌다.
    /// 칸 바깥이 아니라 칸 안쪽 가장자리에 그리므로 이미지가 커지지 않고,
    /// 좌표 기준(World Center / Units To Pixels)도 그대로다.
    /// </summary>
    private static void DrawOutline(Color[] pixels, int width, int height,
                                    bool[] occupied, int tilesWide, int tilesHigh)
    {
        for (int cellY = 0; cellY < tilesHigh; cellY++)
        {
            for (int cellX = 0; cellX < tilesWide; cellX++)
            {
                if (!occupied[cellY * tilesWide + cellX]) continue;

                bool hasLeft = IsFilled(occupied, tilesWide, tilesHigh, cellX - 1, cellY);
                bool hasRight = IsFilled(occupied, tilesWide, tilesHigh, cellX + 1, cellY);
                bool hasBelow = IsFilled(occupied, tilesWide, tilesHigh, cellX, cellY - 1);
                bool hasAbove = IsFilled(occupied, tilesWide, tilesHigh, cellX, cellY + 1);

                int baseX = cellX * PixelsPerTile;
                int baseY = cellY * PixelsPerTile;

                for (int i = 0; i < PixelsPerTile; i++)
                {
                    for (int t = 0; t < OutlineThickness; t++)
                    {
                        if (!hasLeft) Paint(pixels, width, height, baseX + t, baseY + i);
                        if (!hasRight) Paint(pixels, width, height, baseX + PixelsPerTile - 1 - t, baseY + i);
                        if (!hasBelow) Paint(pixels, width, height, baseX + i, baseY + t);
                        if (!hasAbove) Paint(pixels, width, height, baseX + i, baseY + PixelsPerTile - 1 - t);
                    }
                }
            }
        }
    }

    private static bool IsFilled(bool[] occupied, int tilesWide, int tilesHigh, int cellX, int cellY)
    {
        if (cellX < 0 || cellX >= tilesWide || cellY < 0 || cellY >= tilesHigh) return false;

        return occupied[cellY * tilesWide + cellX];
    }

    private static void Paint(Color[] pixels, int width, int height, int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;

        pixels[y * width + x] = OutlineColor;
    }

    private static void FillCell(Color[] pixels, int width, int height, int cellX, int cellY, Color color)
    {
        int startX = cellX * PixelsPerTile;
        int startY = cellY * PixelsPerTile;

        for (int px = 0; px < PixelsPerTile; px++)
        {
            for (int py = 0; py < PixelsPerTile; py++)
            {
                int x = startX + px;
                int y = startY + py;

                if (x < 0 || x >= width || y < 0 || y >= height) continue;

                pixels[y * width + x] = color;
            }
        }
    }

    /// <summary>레이어 이름으로 색을 고른다. 이름이 다르면 회색으로 떨어진다.</summary>
    private static Color ColorFor(string tilemapName)
    {
        string lower = tilemapName.ToLowerInvariant();

        if (lower.Contains("secret")) return SecretColor;
        if (lower.Contains("spike")) return SpikeColor;
        if (lower.Contains("platform")) return PlatformColor;
        if (lower.Contains("ground")) return GroundColor;

        return OtherColor;
    }
}
#endif
