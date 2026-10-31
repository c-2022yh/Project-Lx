using UnityEngine;
using UnityEngine.SceneManagement;

//플레이어가 닿으면 자동으로 발동하는 맵 이동 포탈
public class AutoScenePortal : MonoBehaviour
{
    [Header("Scene Transition")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnId;

    [Header("Audio")]
    [Tooltip("Plays once when scene travel starts. The shared audio service keeps its tail across scene loads.")]
    [SerializeField] private SoundCue transitionSound = new SoundCue();

    private bool isTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTriggered || SceneFader.IsTransitioning) return;

        Player player = other.GetComponentInParent<Player>();

        if (player == null) return;

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("이동할 씬이 지정되지 않았습니다.", this);
            return;
        }

        isTriggered = true;

        SceneTransitionData.TargetSpawnId = targetSpawnId;

        GameAudio.Play(transitionSound, transform.position);
        SceneFader.LoadScene(targetSceneName);
    }
}