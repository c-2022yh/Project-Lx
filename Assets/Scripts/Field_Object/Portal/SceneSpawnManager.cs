using UnityEngine;

// 씬 시작 시 Player를 준비하고 지정된 SpawnPoint로 이동
public class SceneSpawnManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Player playerPrefab;

    [Header("Default Spawn")]
    [SerializeField] private Transform defaultSpawnPoint;

    private void Awake()
    {
        //플레이어 찾기
        Player player = FindFirstObjectByType<Player>();

        //Player가 없으면 최초 생성
        if (player == null)
        {
            Vector3 spawnPosition = defaultSpawnPoint != null ? defaultSpawnPoint.position : Vector3.zero;

            player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        }

        string targetSpawnId = SceneTransitionData.TargetSpawnId;

        //포탈을 통해 들어온 게 아니면 기본 스폰 위치 사용
        if (string.IsNullOrEmpty(targetSpawnId))
        {
            if (defaultSpawnPoint != null)
            {
                player.transform.position = defaultSpawnPoint.position;
            }

            return;
        }

        SceneSpawnPoint[] spawnPoints = FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None);

        foreach (SceneSpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint.SpawnId != targetSpawnId)
                continue;

            player.transform.position = spawnPoint.transform.position;

            SceneTransitionData.TargetSpawnId = null;
            return;
        }


        SceneTransitionData.TargetSpawnId = null;
    }
}