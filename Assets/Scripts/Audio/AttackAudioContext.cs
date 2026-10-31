using System.Collections.Generic;
using UnityEngine;

// One context per basic attack or skill cast; copied DamageInfo values share hit policy.
public sealed class AttackAudioContext
{
    private readonly SoundCue cue;
    private readonly ImpactSoundMode mode;
    private readonly Transform owner;
    private readonly bool suppressOnKill;
    private readonly HashSet<int> targets;
    private bool played;
    private bool clipSelected;
    private SoundCue selectedCue;
    private AudioClip selectedClip;

    public AttackAudioContext(SoundCue cue, ImpactSoundMode mode, Transform owner, bool suppressOnKill = false)
    {
        this.cue = cue;
        this.mode = mode;
        this.owner = owner;
        this.suppressOnKill = suppressOnKill;
        if (mode == ImpactSoundMode.OncePerTarget) targets = new HashSet<int>();
    }

    // Share only the chosen sound. Each hitbox keeps its own hit policy/state.
    public AttackAudioContext CreateLinkedImpact(ImpactSoundMode linkedMode)
    {
        if (!clipSelected)
        {
            selectedCue = GameAudio.ResolveCueForPlayback(cue);
            selectedClip = selectedCue?.PickClip();
            clipSelected = true;
        }
        return new AttackAudioContext(cue, linkedMode, owner, suppressOnKill)
        {
            clipSelected = true,
            selectedCue = this.selectedCue,
            selectedClip = this.selectedClip
        };
    }

    // Call after damage is accepted and HP is updated. Skipped kills do not consume hit policy.
    public void OnDamageApplied(Transform target, bool isLethal = false)
    {
        if (target == null || (suppressOnKill && isLethal)) return;
        if (mode == ImpactSoundMode.OncePerAction && played) return;
        int id = target.GetHashCode();
        if (mode == ImpactSoundMode.OncePerTarget && targets.Contains(id)) return;
        SoundHandle handle = clipSelected
            ? GameAudio.PlaySelectedClip(selectedCue, selectedClip, target.position, owner)
            : GameAudio.Play(cue, target.position, owner);
        if (!handle.IsPlaying) return;
        played = true;
        if (mode == ImpactSoundMode.OncePerTarget) targets.Add(id);
    }
}
