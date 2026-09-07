using UnityEngine;

//씬 시작 시 지정된 SpawnPoint로 플레이어 이동
public class SceneSpawnManager : MonoBehaviour
{
    private void Start()
    {
        string targetSpawnId = SceneTransitionData.TargetSpawnId;

        if (string.IsNullOrEmpty(targetSpawnId)) return;

        Player player = FindFirstObjectByType<Player>();

        if (player == null) return;
        

        SceneSpawnPoint[] spawnPoints = FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None);

        foreach (SceneSpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint.SpawnId != targetSpawnId) continue;

            player.transform.position = spawnPoint.transform.position;

            SceneTransitionData.TargetSpawnId = null;

            return;
        }

    }
}