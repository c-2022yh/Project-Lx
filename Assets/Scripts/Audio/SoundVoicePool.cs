using System.Collections.Generic;
using UnityEngine;

// Runtime state is separate from serializable cues and shared skill assets.
internal sealed class SoundVoice
{
    public AudioSource source;
    public uint generation;
    public bool active;
    public SoundCue cue;
    public int scope;
    public Transform owner;
    public bool stopWithOwner;
    public bool followOwner;

    public void Stop()
    {
        if (source != null) { source.Stop(); source.clip = null; source.loop = false; }
        active = false;
        cue = null;
        owner = null;
    }
}

internal sealed class SoundVoicePool
{
    private readonly SoundVoice[] voices;
    private readonly bool allowSpatial;
    private readonly Dictionary<(SoundCue cue, int scope), float> cooldowns = new Dictionary<(SoundCue, int), float>();
    private readonly List<(SoundCue cue, int scope)> expired = new List<(SoundCue, int)>();

    public SoundVoicePool(AudioSource[] sources, bool allowSpatial)
    {
        this.allowSpatial = allowSpatial;
        voices = new SoundVoice[sources.Length];
        for (int i = 0; i < sources.Length; i++) voices[i] = new SoundVoice { source = sources[i] };
    }

    // Death/explicit cancellation stops owned cast sounds and loops, keeping shot tails.
    public void StopOwned(Transform owner)
    {
        foreach (SoundVoice voice in voices)
            if (voice.active && voice.stopWithOwner && voice.owner == owner) voice.Stop();
    }

    // Reclaim finished voices, stop orphaned loops and follow moving emitters.
    public void Tick()
    {
        foreach (SoundVoice voice in voices)
        {
            if (!voice.active) continue;
            if (voice.source == null || (voice.stopWithOwner && (voice.owner == null || !voice.owner.gameObject.activeInHierarchy)))
            { voice.Stop(); continue; }
            if (!voice.source.isPlaying && (!AudioListener.pause || voice.source.ignoreListenerPause))
            { voice.Stop(); continue; }
            if (voice.followOwner && voice.owner != null) voice.source.transform.position = voice.owner.position;
        }
        expired.Clear();
        foreach (var item in cooldowns)
            if (Time.unscaledTime >= item.Value) expired.Add(item.Key);
        foreach (var key in expired) cooldowns.Remove(key);
    }

    // Enforce cue limits per actor by default, or globally when explicitly requested.
    public SoundHandle Play(SoundCue cue, AudioClip clip, Vector3 position, Transform owner, bool loop, bool stopWithOwner)
    {
        if (loop && (owner == null || !owner.gameObject.activeInHierarchy)) return default;
        Tick();
        int scope = cue.limitScope == SoundLimitScope.Global || owner == null ? 0 : owner.GetHashCode();
        var key = (cue, scope);
        if (cooldowns.TryGetValue(key, out float until) && Time.unscaledTime < until) return default;
        SoundVoice free = null;
        int count = 0;
        foreach (SoundVoice voice in voices)
        {
            if (!voice.active) { if (free == null) free = voice; }
            else if (ReferenceEquals(voice.cue, cue) && voice.scope == scope) count++;
        }
        if (free == null || count >= Mathf.Max(1, cue.maxSimultaneous)) return default;
        free.generation++;
        free.active = true;
        free.cue = cue;
        free.scope = scope;
        free.owner = owner;
        free.stopWithOwner = loop || stopWithOwner;
        free.followOwner = loop || stopWithOwner;
        AudioSource source = free.source;
        source.transform.position = position;
        source.clip = clip;
        source.loop = loop;
        source.volume = Mathf.Clamp01(cue.volume);
        float low = Mathf.Clamp(Mathf.Min(cue.pitchRange.x, cue.pitchRange.y), 0.1f, 3f);
        float high = Mathf.Clamp(Mathf.Max(cue.pitchRange.x, cue.pitchRange.y), 0.1f, 3f);
        source.pitch = Random.Range(low, high);
        source.spatialBlend = allowSpatial ? Mathf.Clamp01(cue.spatialBlend) : 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = Mathf.Max(0.01f, cue.minDistance);
        source.maxDistance = Mathf.Max(source.minDistance + 0.01f, cue.maxDistance);
        source.dopplerLevel = 0f;
        source.Play();
        if (cue.minimumInterval > 0f) cooldowns[key] = Time.unscaledTime + cue.minimumInterval;
        return new SoundHandle(free);
    }
}
