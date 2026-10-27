using UnityEngine;
using UnityEngine.UI;

// Automatically attached at runtime; also usable on dynamically spawned buttons.
[RequireComponent(typeof(Button)), DisallowMultipleComponent]
public sealed class GameAudioButton : MonoBehaviour
{
    [SerializeField] private GameSound clickSound = GameSound.UIClick;
    private Button button;

    // Subscribe once so re-opening a window does not multiply click sounds.
    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClick);
    }

    // Unity invokes this for both pointer clicks and navigation submit.
    private void PlayClick() { GameAudio.Play(clickSound); }

    // Remove only the audio listener owned by this component.
    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(PlayClick);
    }
}
