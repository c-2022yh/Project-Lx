using System.Collections.Generic;
using UnityEngine;

// One context per basic attack or skill cast; copied DamageInfo values share hit policy.
public sealed class AttackAudioContext
{
    private readonly SoundCue cue;
    private readonly ImpactSoundMode mode;
    private readonly Transform owner;
    private readonly HashSet<int> targets;
    private bool played;
    private bool clipSelected;
    private SoundCue selectedCue;
    private AudioClip selectedClip;

    public AttackAudioContext(SoundCue cue, ImpactSoundMode mode, Transform owner)
    {
        this.cue = cue;
        this.mode = mode;
        this.owner = owner;
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
        return new AttackAudioContext(cue, linkedMode, owner)
        {
            clipSelected = true,
            selectedCue = this.selectedCue,
            selectedClip = this.selectedClip
        };
    }

    // Call only after real damage is accepted, including a lethal hit.
    public void OnDamageApplied(Transform target)
    {
        if (target == null) return;
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
