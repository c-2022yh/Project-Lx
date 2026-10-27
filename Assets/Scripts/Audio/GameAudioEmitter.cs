using UnityEngine;

// Optional hook for animation events, triggers and Inspector UnityEvents.
public sealed class GameAudioEmitter : MonoBehaviour
{
    [SerializeField] private GameSound sound = GameSound.PlayerAttack;

    // Choose the slot in the Inspector, then call this from a UnityEvent.
    public void Play() { GameAudio.Play(sound); }
}
