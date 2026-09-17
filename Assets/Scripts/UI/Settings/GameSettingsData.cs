using System;

/// <summary>
/// 설정 화면의 모든 값. PlayerPrefs에 JSON으로 저장된다.
/// 아직 게임에 없는 시스템(오디오/언어)의 값도 미리 담아둔다.
/// 시스템이 생기면 SettingsApplier의 해당 훅만 채우면 된다.
/// </summary>
[Serializable]
public class GameSettingsData
{
    // ── 비디오 ─────────────────────────────
    /// <summary>Screen.resolutions 인덱스. -1이면 현재 해상도 유지.</summary>
    public int resolutionIndex = -1;

    /// <summary>0=전체 화면, 1=전체 창 모드, 2=창 모드</summary>
    /// 기본값을 '전체 창 모드'로 둔다. 독점 전체화면(0)은 실행 직후 해상도를 바꾸면서
    /// 화면이 깜빡이거나 포커스를 잃는 경우가 있어 기본값으로는 위험하다.
    public int screenModeIndex = 1;

    /// <summary>0~100. URP Volume의 Color Adjustments에 물릴 값.</summary>
    public float brightness = 80f;

    public bool vSync = true;

    /// <summary>FrameLimitOptions 인덱스.</summary>
    public int frameLimitIndex = 1;

    // ── 오디오 (0~100) ──────────────────────
    // [AUDIO_HOOK] 현재 프로젝트에 AudioMixer가 없다.
    // 값은 저장/복원되지만 아직 아무 곳에도 적용되지 않는다.
    public float masterVolume = 100f;
    public float bgmVolume = 80f;
    public float sfxVolume = 80f;
    public float uiVolume = 60f;

    // ── 기타 ───────────────────────────────
    /// <summary>0=한국어, 1=English</summary>
    // [LOCALIZATION_HOOK] Localization 패키지 도입 전까지는 값만 보관.
    public int languageIndex = 0;

    public bool cameraShake = true;
    public bool screenFlash = true;
    public bool showDamageNumbers = true;

    public GameSettingsData Clone()
    {
        return (GameSettingsData)MemberwiseClone();
    }
}
