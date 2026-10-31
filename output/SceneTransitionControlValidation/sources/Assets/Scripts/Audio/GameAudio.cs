using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shared 2D playback. Created automatically even when a test scene is opened directly.
public sealed class GameAudio : MonoBehaviour
{
    private static GameAudio instance;
    private GameAudioCatalog catalog;
    private AudioSource[] sfxSources;
    private AudioSource[] uiSources;
    private AudioSource[] musicSources;
    private SoundVoicePool sfxPool;
    private SoundVoicePool uiPool;
    private readonly float[] musicGains = new float[2];
    private int musicIndex;
    private SoundCue currentMusic;
    private Coroutine musicRoutine;
    private bool started;
    private float nextButtonScan;
    private int uiFeedbackFrame = -1;
    private int pendingClickFrame = -1;
    private GameSound pendingClickSound;
    private SoundCue pendingCustomClick;
    private bool pendingClickIsCustom;
    private Vector3 pendingClickPosition;

    // Reset static references when entering Play Mode with domain reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    // A scene object or prefab is not required for the shared audio service.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("GameAudio");
        go.AddComponent<GameAudio>();
    }

    // Load the catalog and create bounded voice pools without starting any sound.
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        catalog = Resources.Load<GameAudioCatalog>(GameAudioCatalog.ResourcePath);
        if (catalog == null)
        {
            Debug.LogWarning("[GameAudio] Catalog missing. Run Project Lx/Audio/Set Up Audio.");
            return;
        }
        sfxSources = CreateSources("SFX", Mathf.Clamp(catalog.sfxVoices, 1, 32), catalog.sfxGroup, false);
        uiSources = CreateSources("UI", Mathf.Clamp(catalog.uiVoices, 1, 16), catalog.uiGroup, true);
        musicSources = CreateSources("BGM", 2, catalog.bgmGroup, true);
        foreach (AudioSource source in musicSources) source.loop = true;
        sfxPool = new SoundVoicePool(sfxSources, true);
        uiPool = new SoundVoicePool(uiSources, false);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Mixer parameters must be set in Start or later, rather than Awake.
    private void Start()
    {
        started = true;
        RefreshVolumes(GameSettings.Current);
        if (catalog == null) return;
        PlaySceneMusic(SceneManager.GetActiveScene());
        BindButtons();
    }

    // Keep dynamically created buttons connected while gameplay is paused too.
    private void Update()
    {
        if (catalog == null) return;
        sfxPool.Tick();
        uiPool.Tick();
        if (Time.unscaledTime >= nextButtonScan)
        {
            nextButtonScan = Time.unscaledTime + 0.25f;
            BindButtons();
        }
    }

    // Resolve generic clicks after button actions, regardless of listener order.
    private void LateUpdate()
    {
        if (pendingClickFrame < 0) return;
        bool play = pendingClickIsCustom || uiFeedbackFrame != pendingClickFrame;
        pendingClickFrame = -1;
        if (!play) return;
        if (pendingClickIsCustom) Play(pendingCustomClick, pendingClickPosition);
        else Play(pendingClickSound);
    }

    internal static void QueueButtonClick(GameSound sound, bool custom, SoundCue cue, Vector3 position)
    {
        if (instance == null) return;
        instance.pendingClickFrame = Time.frameCount;
        instance.pendingClickSound = sound;
        instance.pendingClickIsCustom = custom;
        instance.pendingCustomClick = cue;
        instance.pendingClickPosition = position;
    }

    // Release only this service's scene callback.
    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    // Route scene music and catch inactive menu buttons before they open.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) PlaySceneMusic(scene);
        BindButtons();
    }

    // Title and gameplay music are shared defaults; empty destinations stop old music.
    private void PlaySceneMusic(Scene scene)
    {
        if (!catalog.autoPlaySceneMusic) return;
        if (catalog.sceneMusic != null)
        {
            foreach (SceneMusicEntry entry in catalog.sceneMusic)
            {
                if (entry == null || entry.sceneName != scene.name) continue;
                SoundCue cue = entry.music != null ? Resolve(entry.music) : null;
                AudioClip clip = cue?.PickClip();
                if (clip == null) StopMusic();
                else PlayMusic(cue, clip);
                return;
            }
        }
        Play(scene.name == GameFlow.TitleSceneName ? GameSound.TitleBgm : GameSound.GameplayBgm);
    }

    // Attach a small listener without changing scene/prefab assets or button actions.
    private void BindButtons()
    {
        if (!catalog.autoBindButtons) return;
        foreach (Selectable control in FindObjectsByType<Selectable>(FindObjectsInactive.Include))
            if (!(control is Scrollbar) && control.GetComponent<GameAudioButton>() == null)
                control.gameObject.AddComponent<GameAudioButton>();
    }

    // Every source is 2D and routed through its category mixer group.
    private AudioSource[] CreateSources(string label, int count, UnityEngine.Audio.AudioMixerGroup group, bool ignorePause)
    {
        var sources = new AudioSource[count];
        for (int i = 0; i < count; i++)
        {
            var child = new GameObject(label + " " + (i + 1));
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = group;
            source.ignoreListenerPause = ignorePause;
            sources[i] = source;
        }
        return sources;
    }

    // Shared UI/music and explicitly selected legacy fallback events use this overload.
    public static SoundHandle Play(GameSound sound)
    {
        if (instance == null || instance.catalog == null) return default;
        if ((sound >= GameSound.UIEquip && sound <= GameSound.UITabChange)
            || (sound >= GameSound.UIEquipSword && sound <= GameSound.UIEquipBody))
            instance.uiFeedbackFrame = Time.frameCount;
        return Play(instance.catalog.Find(sound), Vector3.zero);
    }

    // Direct cue playback lets patterns, skill assets and prefabs own their clips.
    public static SoundHandle Play(SoundCue cue, Vector3 position, Transform owner = null, bool stopWithOwner = false)
    {
        if (instance == null || instance.catalog == null || cue == null) return default;
        SoundCue resolved = instance.Resolve(cue);
        AudioClip clip = resolved?.PickClip();
        if (resolved == null)
        {
            if (cue.channel == GameAudioChannel.BGM) StopMusic();
            return default;
        }
        return PlaySelectedClip(resolved, clip, position, owner, stopWithOwner);
    }

    // Keep the original cue identity so interval/voice limits still apply.
    internal static SoundCue ResolveCueForPlayback(SoundCue cue)
    {
        if (cue == null) return null;
        return cue.HasClip ? cue : instance != null && instance.catalog != null ? instance.Resolve(cue) : null;
    }

    internal static SoundHandle PlaySelectedClip(SoundCue resolved, AudioClip clip, Vector3 position,
        Transform owner, bool stopWithOwner = false)
    {
        if (instance == null || instance.catalog == null || resolved == null) return default;
        if (clip == null)
        {
            if (resolved.channel == GameAudioChannel.BGM) StopMusic();
            return default;
        }
        if (resolved.channel == GameAudioChannel.BGM)
        {
            instance.PlayMusic(resolved, clip);
            return default;
        }
        SoundVoicePool pool = resolved.channel == GameAudioChannel.UI ? instance.uiPool : instance.sfxPool;
        return pool.Play(resolved, clip, position, owner, false, stopWithOwner);
    }

    // Loops require an owner and are automatically stopped when it disappears.
    public static SoundHandle PlayLoop(SoundCue cue, Transform owner)
    {
        if (instance == null || instance.catalog == null || cue == null || owner == null) return default;
        SoundCue resolved = instance.Resolve(cue);
        AudioClip clip = resolved?.PickClip();
        if (clip == null || resolved.channel == GameAudioChannel.BGM) return default;
        SoundVoicePool pool = resolved.channel == GameAudioChannel.UI ? instance.uiPool : instance.sfxPool;
        return pool.Play(resolved, clip, owner.position, owner, true, true);
    }

    // Empty local slots stay silent unless their fallback toggle is explicitly enabled.
    private SoundCue Resolve(SoundCue cue)
    {
        if (cue.HasClip) return cue;
        if (!cue.useFallback) return null;
        GameSoundSlot fallback = catalog.Find(cue.fallbackSound);
        return fallback != null && fallback.HasClip ? fallback : null;
    }

    // Stop an actor's cast/loop voices on death without truncating independent impacts.
    public static void StopOwnedSounds(Transform owner)
    {
        if (instance == null || owner == null) return;
        instance.sfxPool?.StopOwned(owner);
        instance.uiPool?.StopOwned(owner);
    }

    // Keep shared cues or identical clips running across scenes without restarting playback.
    private void PlayMusic(SoundCue cue, AudioClip clip)
    {
        AudioSource current = musicSources[musicIndex];
        if (currentMusic != null && current.clip != null
            && (ReferenceEquals(currentMusic, cue) || current.clip == clip)) return;
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicIndex = 1 - musicIndex;
        AudioSource target = musicSources[musicIndex];
        target.Stop();
        target.clip = clip;
        target.pitch = 1f;
        musicGains[musicIndex] = 0f;
        target.volume = 0f;
        target.Play();
        currentMusic = cue;
        musicRoutine = StartCoroutine(FadeMusic(musicIndex, Mathf.Clamp01(cue.volume)));
    }

    // Fade using unscaled time so transitions also finish while menus pause gameplay.
    private IEnumerator FadeMusic(int targetIndex, float targetGain)
    {
        float a = musicGains[0];
        float b = musicGains[1];
        float duration = Mathf.Max(0f, catalog.musicFadeSeconds);
        float elapsed = 0f;
        do
        {
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            musicGains[0] = Mathf.Lerp(a, targetIndex == 0 ? targetGain : 0f, t);
            musicGains[1] = Mathf.Lerp(b, targetIndex == 1 ? targetGain : 0f, t);
            musicSources[0].volume = musicGains[0];
            musicSources[1].volume = musicGains[1];
            if (t >= 1f) break;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        } while (true);
        for (int i = 0; i < 2; i++)
            if (targetIndex != i) { musicSources[i].Stop(); musicSources[i].clip = null; }
        musicRoutine = null;
    }

    // Stop music without interrupting UI or gameplay effect voices.
    public static void StopMusic()
    {
        if (instance == null || instance.musicSources == null) return;
        if (instance.musicRoutine != null) instance.StopCoroutine(instance.musicRoutine);
        instance.currentMusic = null;
        instance.musicRoutine = instance.StartCoroutine(instance.FadeMusic(-1, 0f));
    }

    // Existing settings remain the single source for all four volume sliders.
    public static void ApplySettings(GameSettingsData settings)
    {
        if (instance != null && instance.started) instance.RefreshVolumes(settings);
    }

    // Apply exposed mixer parameters and use mute flags for an exact zero volume.
    private void RefreshVolumes(GameSettingsData settings)
    {
        if (catalog == null || settings == null) return;
        if (catalog.mixer != null)
        {
            catalog.mixer.SetFloat("Master", SettingsApplier.ToDecibel(settings.masterVolume));
            catalog.mixer.SetFloat("BGM", SettingsApplier.ToDecibel(settings.bgmVolume));
            catalog.mixer.SetFloat("SFX", SettingsApplier.ToDecibel(settings.sfxVolume));
            catalog.mixer.SetFloat("UI", SettingsApplier.ToDecibel(settings.uiVolume));
        }
        foreach (AudioSource source in sfxSources) source.mute = settings.masterVolume <= 0f || settings.sfxVolume <= 0f;
        foreach (AudioSource source in uiSources) source.mute = settings.masterVolume <= 0f || settings.uiVolume <= 0f;
        foreach (AudioSource source in musicSources) source.mute = settings.masterVolume <= 0f || settings.bgmVolume <= 0f;
    }
}
