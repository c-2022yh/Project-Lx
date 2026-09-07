using UnityEngine;

//씬 이동 후 플레이어가 등장할 위치
public class SceneSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId;

    public string SpawnId => spawnId;
}