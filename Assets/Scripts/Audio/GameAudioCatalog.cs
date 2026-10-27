using System;
using UnityEngine;
using UnityEngine.Audio;

public enum GameSound
{
    PlayerAttack = 0, PlayerHurt = 1, PlayerDeath = 2, EnemyHurt = 3, EnemyDeath = 4,
    UIClick = 5, WindowOpen = 6, WindowClose = 7, TitleBgm = 8, GameplayBgm = 9
}

public enum GameAudioChannel { SFX, UI, BGM }

// Existing serialized clip/volume fields are preserved through inheritance.
[Serializable]
public class GameSoundSlot : SoundCue
{
    public GameSound sound;
}

[Serializable]
public class SceneMusicEntry
{
    public string sceneName;
    public SoundCue music = new SoundCue(GameAudioChannel.BGM);
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

    [Header("Optional scene music overrides (exact scene name)")]
    public SceneMusicEntry[] sceneMusic = Array.Empty<SceneMusicEntry>();

    [Header("Shared UI / BGM and optional legacy fallbacks")]
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
            if ((int)sound < (int)GameSound.UIClick || Find(sound) != null) continue;
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
