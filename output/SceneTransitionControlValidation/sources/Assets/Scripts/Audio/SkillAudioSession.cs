using System;
using UnityEngine;

[Serializable]
public sealed class SkillSoundSettings
{
    public SoundCue castSound = new SoundCue();
    public SoundCue releaseSound = new SoundCue();
    public SoundCue impactSound = new SoundCue();
    public ImpactSoundMode impactMode = ImpactSoundMode.EveryHit;
    public SoundCue activeLoop = new SoundCue();
    public SoundCue completeSound = new SoundCue();
    public SoundCue cancelSound = new SoundCue();
}

// Owned by PlayerSkill, never stored back into the shared SkillData asset.
public sealed class SkillAudioSession
{
    private readonly SkillSoundSettings settings;
    private readonly Transform owner;
    private SoundHandle cast;
    private SoundHandle loop;
    private bool finished;
    private bool active;
    public AttackAudioContext Impact { get; }

    public SkillAudioSession(SkillSoundSettings settings, Transform owner)
    {
        this.settings = settings;
        this.owner = owner;
        Impact = new AttackAudioContext(settings?.impactSound, settings != null ? settings.impactMode : ImpactSoundMode.EveryHit, owner);
    }

    public void BeginCast()
    {
        if (!finished && settings != null && owner != null)
            cast = GameAudio.Play(settings.castSound, owner.position, owner, true);
    }

    public void BeginActive()
    {
        if (active || finished) return;
        active = true;
        cast.Stop();
        if (!finished && settings != null && owner != null)
            loop = GameAudio.PlayLoop(settings.activeLoop, owner);
    }

    // Each skill calls this at its actual effect/release point, including each slash.
    public void Release(Vector3 position)
    {
        BeginActive();
        if (!finished && settings != null) GameAudio.Play(settings.releaseSound, position, owner);
    }

    public void EndWindup() { cast.Stop(); }
    public void EndActive() { cast.Stop(); loop.Stop(); }

    public void Finish(bool cancelled)
    {
        if (finished) return;
        finished = true;
        EndActive();
        if (settings != null && owner != null && owner.gameObject.activeInHierarchy)
            GameAudio.Play(cancelled ? settings.cancelSound : settings.completeSound, owner.position, owner);
    }
}
