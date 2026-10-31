using System;
using UnityEngine;

public enum SoundLimitScope { PerOwner, Global }
public enum ImpactSoundMode { EveryHit, OncePerAction, OncePerTarget }

// Serializable settings only. Playback state belongs to the runtime service.
[Serializable]
public class SoundCue
{
    public GameAudioChannel channel = GameAudioChannel.SFX;
    [Tooltip("Drop the main clip here. Clear it to use the random Clips variants below.")]
    public AudioClip clip;
    [Tooltip("One clip, or several random variants. Empty means silent unless fallback is enabled.")]
    public AudioClip[] clips = new AudioClip[1];
    [Range(0f, 1f)] public float volume = 1f;
    [Min(0f)] public float minimumInterval = 0.05f;
    public Vector2 pitchRange = Vector2.one;
    [Range(1, 32)] public int maxSimultaneous = 4;
    public SoundLimitScope limitScope = SoundLimitScope.PerOwner;
    [Header("Optional SFX distance attenuation")]
    [Range(0f, 1f)] public float spatialBlend;
    [Min(0.01f)] public float minDistance = 1f;
    [Min(0.01f)] public float maxDistance = 15f;
    [Header("Optional shared fallback when Clips is empty")]
    public bool useFallback;
    public GameSound fallbackSound = GameSound.UIClick;

    public SoundCue() { }
    public SoundCue(GameAudioChannel category) { channel = category; }

    // Count valid clips so empty entries do not bias random variation.
    public AudioClip PickClip()
    {
        if (clip != null) return clip;
        if (clips == null) return null;
        int count = 0;
        foreach (AudioClip clip in clips) if (clip != null) count++;
        if (count == 0) return null;
        int index = UnityEngine.Random.Range(0, count);
        foreach (AudioClip clip in clips)
            if (clip != null && index-- == 0) return clip;
        return null;
    }

    public bool HasClip
    {
        get
        {
            if (clip != null) return true;
            if (clips != null) foreach (AudioClip clip in clips) if (clip != null) return true;
            return false;
        }
    }
}

// Generation checks prevent an old handle from stopping a reused voice.
public readonly struct SoundHandle
{
    private readonly SoundVoice voice;
    private readonly uint generation;

    internal SoundHandle(SoundVoice voice)
    {
        this.voice = voice;
        generation = voice.generation;
    }

    public bool IsPlaying => voice != null && voice.generation == generation && voice.active && voice.source != null
        && (voice.source.isPlaying || (AudioListener.pause && !voice.source.ignoreListenerPause));

    public void Stop()
    {
        if (voice != null && voice.generation == generation) voice.Stop();
    }
}
