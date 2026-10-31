using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Shared pointer feedback for standard UI controls, plus click feedback for buttons.
[RequireComponent(typeof(Selectable)), DisallowMultipleComponent]
public sealed class GameAudioButton : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private GameSound clickSound = GameSound.UIClick;
    [SerializeField] private bool useCustomSound;
    [SerializeField] private SoundCue customSound = new SoundCue(GameAudioChannel.UI);
    private Button button;
    private Selectable control;

    // Subscribe once so re-opening a window does not multiply click sounds.
    private void Awake()
    {
        control = GetComponent<Selectable>();
        button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(PlayClick);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || control == null || !control.IsActive() || !control.IsInteractable()) return;
        if (control is Scrollbar) return;
        GameAudio.Play(GameSound.UIHover);
    }

    // Unity invokes this for both pointer clicks and navigation submit.
    private void PlayClick()
    {
        if (!enabled) return;
        GameAudio.QueueButtonClick(clickSound, useCustomSound, customSound, transform.position);
    }

    // Remove only the audio listener owned by this component.
    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(PlayClick);
    }
}
