using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

//씬 이동 후 플레이어가 등장할 위치
public class SceneSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId;

    public string SpawnId => spawnId;

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(0.8f, 1.8f, 0f));

        Handles.Label(transform.position + Vector3.up * 1.2f, spawnId);
    }
    #endif
}

