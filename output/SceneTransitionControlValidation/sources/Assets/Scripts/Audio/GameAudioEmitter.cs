using UnityEngine;

// Optional hook for animation events, triggers and Inspector UnityEvents.
public sealed class GameAudioEmitter : MonoBehaviour
{
    [SerializeField] private GameSound sound = GameSound.PlayerAttack;

    [SerializeField] private bool useCustomSound = true;
    [SerializeField] private SoundCue customSound = new SoundCue();
    [SerializeField] private bool loop;
    [SerializeField] private bool playOnEnable;
    private SoundHandle handle;

    // Local cues support animation events and object-owned ambient/effect loops.
    public void Play()
    {
        if (!useCustomSound) { GameAudio.Play(sound); return; }
        if (loop) { handle.Stop(); handle = GameAudio.PlayLoop(customSound, transform); }
        else handle = GameAudio.Play(customSound, transform.position, transform);
    }

    public void Stop() { handle.Stop(); }
    private void OnEnable() { if (playOnEnable) Play(); }
    private void OnDisable() { Stop(); }
}
