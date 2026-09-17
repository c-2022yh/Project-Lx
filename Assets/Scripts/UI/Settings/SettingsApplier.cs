using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 설정값을 실제 엔진/시스템에 반영하는 곳.
///
/// 지금 동작하는 것:  해상도, 화면 모드, VSync, 프레임 제한
/// 값만 보관 중인 것:  밝기, 오디오 4채널, 언어
///
/// 시스템이 생기면 아래 [*_HOOK] 주석 부분만 채우면 된다.
/// 다른 파일은 손댈 필요 없다.
/// </summary>
public static class SettingsApplier
{
    public static void ApplyAll(GameSettingsData d)
    {
        if (d == null) return;

        ApplyVideo(d);
        ApplyBrightness(d);
        ApplyAudio(d);
        ApplyLanguage(d);
        // 카메라 흔들림 / 화면 플래시 / 데미지 숫자는 "밀어넣는" 값이 아니라
        // 각 시스템이 GameSettings에서 "읽어가는" 값이라 여기서 할 일이 없다.
    }

    // ── 비디오 ─────────────────────────────
    private static void ApplyVideo(GameSettingsData d)
    {
#pragma warning disable CS0219 // 에디터 빌드에서는 mode가 쓰이지 않는다
        FullScreenMode mode = d.screenModeIndex switch
        {
            0 => FullScreenMode.ExclusiveFullScreen,
            1 => FullScreenMode.FullScreenWindow,
            _ => FullScreenMode.Windowed
        };

#if !UNITY_EDITOR
        // 에디터에서는 해상도/전체화면을 건드리지 않는다.
        // Game 뷰 해상도가 멋대로 바뀌어 테스트를 방해하기 때문.
        List<Vector2Int> resolutions = GameSettings.GetResolutions();

        if (d.resolutionIndex >= 0 && d.resolutionIndex < resolutions.Count)
        {
            Vector2Int res = resolutions[d.resolutionIndex];
            Screen.SetResolution(res.x, res.y, mode);
        }
        else
        {
            Screen.fullScreenMode = mode;
        }
#endif

#pragma warning restore CS0219

        QualitySettings.vSyncCount = d.vSync ? 1 : 0;

        // VSync가 켜져 있으면 targetFrameRate는 무시된다. 그래도 값은 세팅해둔다.
        int limit = -1;
        if (d.frameLimitIndex >= 0 && d.frameLimitIndex < GameSettings.FrameLimitValues.Length)
            limit = GameSettings.FrameLimitValues[d.frameLimitIndex];

        Application.targetFrameRate = limit;
    }

    // ── 밝기 ───────────────────────────────
    private static void ApplyBrightness(GameSettingsData d)
    {
        // [BRIGHTNESS_HOOK]
        // 이 프로젝트는 URP를 쓰므로 Global Volume에 Color Adjustments를 추가하고
        // Post Exposure를 여기서 조절하는 게 가장 깔끔하다.
        //
        //   if (globalVolume.profile.TryGet(out ColorAdjustments ca))
        //       ca.postExposure.value = Mathf.Lerp(-1.5f, 1.5f, d.brightness / 100f);
        //
        // 지금은 값만 보관한다.
    }

    // ── 오디오 ─────────────────────────────
    private static void ApplyAudio(GameSettingsData d)
    {
        // [AUDIO_HOOK]
        // 프로젝트에 AudioMixer가 생기면 여기만 채우면 된다.
        // 믹서 노출 파라미터 이름을 Master / BGM / SFX / UI 로 만들어두면:
        //
        //   mixer.SetFloat("Master", ToDecibel(d.masterVolume));
        //   mixer.SetFloat("BGM",    ToDecibel(d.bgmVolume));
        //   mixer.SetFloat("SFX",    ToDecibel(d.sfxVolume));
        //   mixer.SetFloat("UI",     ToDecibel(d.uiVolume));
        //
        // 지금은 값만 보관한다.
    }

    /// <summary>0~100 슬라이더 값을 AudioMixer용 데시벨로. 오디오 붙일 때 바로 쓰면 된다.</summary>
    public static float ToDecibel(float percent01To100)
    {
        float linear = Mathf.Clamp(percent01To100 / 100f, 0.0001f, 1f);
        return Mathf.Log10(linear) * 20f;
    }

    // ── 언어 ───────────────────────────────
    private static void ApplyLanguage(GameSettingsData d)
    {
        // [LOCALIZATION_HOOK]
        // com.unity.localization 도입 후:
        //   LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[d.languageIndex];
        //
        // 지금은 값만 보관한다.
    }
}
