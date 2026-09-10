using UnityEngine;
using UnityEngine.SceneManagement;

//플레이어가 닿으면 자동으로 발동하는 맵 이동 포탈
public class AutoScenePortal : MonoBehaviour
{
    [Header("Scene Transition")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnId;

    private bool isTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTriggered) return;

        Player player = other.GetComponentInParent<Player>();

        if (player == null) return;

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("이동할 씬이 지정되지 않았습니다.", this);
            return;
        }

        isTriggered = true;

        SceneTransitionData.TargetSpawnId = targetSpawnId;

        SceneManager.LoadScene(targetSceneName);
    }
}