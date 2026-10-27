using System;
using UnityEngine;
using UnityEngine.Audio;

public enum GameSound
{
    PlayerAttack = 0, PlayerHurt = 1, PlayerDeath = 2, EnemyHurt = 3, EnemyDeath = 4,
    UIClick = 5, WindowOpen = 6, WindowClose = 7, TitleBgm = 8, GameplayBgm = 9
}

public enum GameAudioChannel { SFX, UI, BGM }

[Serializable]
public class GameSoundSlot
{
    public GameSound sound;
    public GameAudioChannel channel;
    [Tooltip("Assign one clip, or several variants. Empty slots are silent.")]
    public AudioClip[] clips = new AudioClip[1];
    [Range(0f, 1f)] public float volume = 1f;
    [Min(0f)] public float minimumInterval = 0.05f;
    [Tooltip("Keep both values at 1 for the original pitch.")]
    public Vector2 pitchRange = Vector2.one;

    // Pick a usable variant without allocating during playback.
    public AudioClip PickClip()
    {
        if (clips == null || clips.Length == 0) return null;
        int start = UnityEngine.Random.Range(0, clips.Length);
        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[(start + i) % clips.Length];
            if (clip != null) return clip;
        }
        return null;
    }
}

[CreateAssetMenu(menuName = "Project Lx/Audio Catalog")]
public class GameAudioCatalog : ScriptableObject
{
    public const string ResourcePath = "Audio/GameAudioCatalog";

    [Header("Mixer (created by the Audio setup menu)")]
    public AudioMixer mixer;
    public AudioMixerGroup bgmGroup;
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup uiGroup;

    [Header("Playback")]
    [Range(1, 32)] public int sfxVoices = 16;
    [Range(1, 16)] public int uiVoices = 4;
    [Min(0f)] public float musicFadeSeconds = 0.4f;
    public bool autoPlaySceneMusic = true;
    public bool autoBindButtons = true;

    [Header("Drop audio clips into these slots")]
    public GameSoundSlot[] sounds;

    // Resolve shared event slots from the Inspector catalog.
    public GameSoundSlot Find(GameSound sound)
    {
        if (sounds == null) return null;
        foreach (GameSoundSlot slot in sounds)
            if (slot != null && slot.sound == sound) return slot;
        return null;
    }

    // Create only missing slots so setup can be run again safely.
    public void AddMissingSlots()
    {
        var slots = new System.Collections.Generic.List<GameSoundSlot>(sounds ?? Array.Empty<GameSoundSlot>());
        foreach (GameSound sound in Enum.GetValues(typeof(GameSound)))
        {
            if (Find(sound) != null) continue;
            bool music = sound == GameSound.TitleBgm || sound == GameSound.GameplayBgm;
            bool ui = sound == GameSound.UIClick || sound == GameSound.WindowOpen || sound == GameSound.WindowClose;
            slots.Add(new GameSoundSlot
            {
                sound = sound,
                channel = music ? GameAudioChannel.BGM : ui ? GameAudioChannel.UI : GameAudioChannel.SFX,
                minimumInterval = music ? 0f : 0.05f
            });
        }
        sounds = slots.ToArray();
    }
}
