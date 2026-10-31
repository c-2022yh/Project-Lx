using UnityEngine;
using UnityEngine.SceneManagement;

//상호작용 가능한 포탈
public class ScenePortal : MonoBehaviour, IInteractable
{
    [Header("Scene Transition")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnId;

    [Header("Audio")]
    [Tooltip("Plays once when scene travel starts. The shared audio service keeps its tail across scene loads.")]
    [SerializeField] private SoundCue transitionSound = new SoundCue();

    private Player player;

    public string InteractionText => "이동";

    public void Interact(Player player)
    {
        if (SceneFader.IsTransitioning) return;
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("이동할 씬이 지정되지 않았습니다.", this);
            return;
        }

        SceneTransitionData.TargetSpawnId = targetSpawnId;

        GameAudio.Play(transitionSound, transform.position);
        SceneFader.LoadScene(targetSceneName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player foundPlayer = other.GetComponentInParent<Player>();

        if (foundPlayer == null) return;

        player = foundPlayer;
        player.SetInteractable(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Player foundPlayer = other.GetComponentInParent<Player>();

        if (foundPlayer == null || foundPlayer != player) return;

        player.ClearInteractable(this);
        player = null;
    }
}