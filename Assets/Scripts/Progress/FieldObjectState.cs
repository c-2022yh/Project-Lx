using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 필드 오브젝트(부순 벽, 먹은 보주, 연 상자 등)를 이미 처리했는지 기억한다.
///
/// RunState flag에 넣으니 세이브에 그대로 저장된다.
///   오브젝트: "obj:씬/타입/x,y"  (x, y는 위치를 0.1 단위로 반올림한 정수. 12.34 → 123)
///   타일:     "tile:씬/x,y"      (비밀벽 타일 칸 좌표)
///
/// 위치가 곧 id라서, 씬에서 오브젝트를 옮기면 예전 기록과 연결이 끊긴다.
/// </summary>
public static class FieldObjectState
{
    public const string ObjectPrefix = "obj:";
    public const string TilePrefix = "tile:";

    // ── 오브젝트 ──────────────────────────
    public static string IdOf(Component c)
        => IdOf(c.gameObject.scene.name, c.GetType().Name, c.transform.position);

    public static string IdOf(string scene, string type, Vector2 position)
        => $"{ObjectPrefix}{scene}/{type}/{Tenths(position.x)},{Tenths(position.y)}";

    public static bool IsDone(Component c) => c != null && RunState.Current.HasFlag(IdOf(c));

    public static void MarkDone(Component c)
    {
        if (c != null) RunState.Current.SetFlag(IdOf(c));
    }

    // ── 타일 ──────────────────────────────
    public static string TileId(string scene, Vector3Int cell) => $"{TilePrefix}{scene}/{cell.x},{cell.y}";

    public static void MarkTileBroken(string scene, Vector3Int cell) => RunState.Current.SetFlag(TileId(scene, cell));

    /// <summary>이 씬에서 부서진 타일 칸 목록.</summary>
    public static List<Vector3Int> BrokenTiles(string scene)
    {
        List<Vector3Int> result = new List<Vector3Int>();
        string prefix = $"{TilePrefix}{scene}/";
        foreach (string flag in RunState.Current.Flags)
        {
            if (!flag.StartsWith(prefix, StringComparison.Ordinal)) continue;
            string[] xy = flag.Substring(prefix.Length).Split(',');
            if (xy.Length == 2 && int.TryParse(xy[0], out int x) && int.TryParse(xy[1], out int y))
                result.Add(new Vector3Int(x, y, 0));
        }
        return result;
    }

    private static int Tenths(float v) => Mathf.RoundToInt(v * 10f);
}
