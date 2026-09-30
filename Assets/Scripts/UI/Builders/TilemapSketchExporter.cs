#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
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
/// 뽑고 나면 MapPanel에 넣을 값 두 개(World Center / Units To Pixels)를 같이 알려준다.
/// </summary>
public static class TilemapSketchExporter
{
    /// <summary>타일 한 칸을 몇 픽셀로 그릴지. 이 값이 그대로 MapPanel의 Units To Pixels가 된다.</summary>
    private const int PixelsPerTile = 8;

    /// <summary>너무 큰 맵에서 메모리를 통째로 먹지 않도록 거는 상한.</summary>
    private const int MaxTextureSide = 8192;

    private const string OutputFolder = "Assets/Art/Map";

    private static readonly Color GroundColor = new Color(0.82f, 0.82f, 0.86f, 1f);
    private static readonly Color PlatformColor = new Color(0.55f, 0.60f, 0.70f, 1f);
    private static readonly Color SpikeColor = new Color(0.85f, 0.35f, 0.35f, 1f);
    private static readonly Color SecretColor = new Color(0.60f, 0.45f, 0.80f, 1f);
    private static readonly Color OtherColor = new Color(0.45f, 0.45f, 0.50f, 1f);

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

        foreach (Tilemap map in used)
        {
            Color color = ColorFor(map.gameObject.name);
            BoundsInt bounds = map.cellBounds;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    if (!map.HasTile(new Vector3Int(x, y, 0))) continue;

                    FillCell(pixels, width, height, x - minX, y - minY, color);
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Directory.CreateDirectory(OutputFolder);

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName)) sceneName = "Scene";

        string path = OutputFolder + "/" + sceneName + "_sketch.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
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

        EditorUtility.DisplayDialog("맵 밑그림 완성",
            path + "\n\n" +
            $"이미지 크기  {width} x {height} px  (타일 {tilesWide} x {tilesHigh})\n" +
            $"타일맵 {used.Count}개를 합쳤습니다.\n\n" +
            "MapPanel에 그대로 넣을 값입니다.\n" +
            $"    World Center     X {centerX}   Y {centerY}\n" +
            $"    Units To Pixels  {PixelsPerTile}\n\n" +
            "이 이미지를 디자인 담당에게 밑그림으로 넘기고,\n" +
            "같은 크기 캔버스에 그려서 돌려받으면 좌표가 그대로 맞습니다.",
            "확인");

        Debug.Log($"[TilemapSketchExporter] {path} / World Center ({centerX}, {centerY}) / Units To Pixels {PixelsPerTile}");
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
