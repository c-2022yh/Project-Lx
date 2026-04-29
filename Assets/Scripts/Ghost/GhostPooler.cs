using System.Collections.Generic;
using UnityEngine;

public class GhostPooler : MonoBehaviour
{
    public static GhostPooler Instance;

    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private int poolSize = 30;
    private List<GameObject> pool = new List<GameObject>();

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(ghostPrefab);

            // 하이어라키 정리를 위해 매니저 자식으로 넣기
            obj.transform.SetParent(this.transform);

            obj.SetActive(false);
            pool.Add(obj);
        }
    }

    public GameObject GetGhost()
    {
        foreach (var ghost in pool)
        {
            if (!ghost.activeInHierarchy) return ghost;
        }

        GameObject newGhost = Instantiate(ghostPrefab);
        newGhost.transform.SetParent(this.transform);
        newGhost.SetActive(false);
        pool.Add(newGhost);
        return newGhost;
    }
}