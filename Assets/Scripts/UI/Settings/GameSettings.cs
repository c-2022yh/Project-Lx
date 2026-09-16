using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 설정값의 단일 출처(single source of truth).
/// 어디서든 GameSettings.Current로 읽고, Apply()로 반영한다.
///
/// 저장: PlayerPrefs에 JSON 한 덩어리.
/// 세이브 시스템이 생겨도 이건 그대로 두면 된다 —
/// 설정은 세이브 슬롯과 무관한 "기기 단위" 값이기 때문.
/// </summary>
public static class GameSettings
{
    private const string PrefsKey = "LudensProject.Settings";

    private static GameSettingsData current;

    /// <summary>설정이 적용될 때마다 발행. 각 시스템이 여기 구독해서 자기 값을 읽어가면 된다.</summary>
    public static event Action<GameSettingsData> OnApplied;

    public static GameSettingsData Current
    {
        get
        {
            if (current == null) Load();
            return current;
        }
    }

    // ── 자주 쓰는 값의 바로가기 ──────────────
    // 다른 시스템이 GameSettings.CameraShake 처럼 짧게 읽도록.
    public static bool CameraShake => Current.cameraShake;
    public static bool ScreenFlash => Current.screenFlash;
    public static bool ShowDamageNumbers => Current.showDamageNumbers;

    // ── 선택지 목록 ────────────────────────
    public static readonly string[] ScreenModeLabels = { "전체 화면", "전체 창 모드", "창 모드" };

    public static readonly int[] FrameLimitValues = { 30, 60, 120, 144, -1 };
    public static readonly string[] FrameLimitLabels = { "30 FPS", "60 FPS", "120 FPS", "144 FPS", "무제한" };

    public static readonly string[] LanguageLabels = { "한국어", "English" };

    /// <summary>중복을 제거한 해상도 목록 (너비×높이 기준, 오름차순).</summary>
    public static List<Vector2Int> GetResolutions()
    {
        List<Vector2Int> list = new List<Vector2Int>();
        HashSet<long> seen = new HashSet<long>();

        foreach (Resolution r in Screen.resolutions)
        {
            long key = ((long)r.width << 32) | (uint)r.height;
            if (!seen.Add(key)) continue;
            list.Add(new Vector2Int(r.width, r.height));
        }

        // 에디터에서 Screen.resolutions가 비는 경우 대비
        if (list.Count == 0)
        {
            list.Add(new Vector2Int(1280, 720));
            list.Add(new Vector2Int(1920, 1080));
            list.Add(new Vector2Int(2560, 1440));
        }

        list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        return list;
    }

    public static string FormatResolution(Vector2Int res)
    {
        return $"{res.x} x {res.y} ({AspectLabel(res)})";
    }

    private static string AspectLabel(Vector2Int res)
    {
        if (res.y == 0) return "?";
        float ratio = (float)res.x / res.y;
        if (Mathf.Abs(ratio - 16f / 9f) < 0.02f) return "16:9";
        if (Mathf.Abs(ratio - 16f / 10f) < 0.02f) return "16:10";
        if (Mathf.Abs(ratio - 4f / 3f) < 0.02f) return "4:3";
        if (Mathf.Abs(ratio - 21f / 9f) < 0.05f) return "21:9";
        return $"{ratio:0.00}:1";
    }

    // ── 저장 / 불러오기 ─────────────────────
    public static void Load()
    {
        string json = PlayerPrefs.GetString(PrefsKey, string.Empty);

        if (string.IsNullOrEmpty(json))
        {
            current = new GameSettingsData();
            return;
        }

        try
        {
            current = JsonUtility.FromJson<GameSettingsData>(json) ?? new GameSettingsData();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameSettings] 설정을 읽지 못해 기본값으로 시작합니다: {e.Message}");
            current = new GameSettingsData();
        }
    }

    public static void Save()
    {
        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(Current));
        PlayerPrefs.Save();
    }

    public static void ResetToDefault()
    {
        current = new GameSettingsData();
    }

    /// <summary>설정 화면의 "적용" 버튼이 호출. 실제 반영 + 저장.</summary>
    public static void Apply()
    {
        SettingsApplier.ApplyAll(Current);
        Save();
        OnApplied?.Invoke(Current);
    }

    /// <summary>게임 시작 시 한 번. 저장된 설정을 읽어 반영한다.</summary>
    public static void LoadAndApply()
    {
        Load();
        SettingsApplier.ApplyAll(Current);
        OnApplied?.Invoke(Current);
    }
}
