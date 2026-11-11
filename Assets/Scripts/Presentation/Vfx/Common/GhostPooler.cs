using System.Collections.Generic;
using UnityEngine;

//잔상 생성 관리 오브젝트 풀러
public class GhostPooler : MonoBehaviour
{
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private int poolSize = 30;

    private List<GameObject> pool = new List<GameObject>();

    private void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(ghostPrefab, transform);

            obj.SetActive(false);
            pool.Add(obj);
        }
    }

    public GameObject GetGhost()
    {
        foreach (GameObject ghost in pool)
        {
            if (ghost == null) continue;
            if (!ghost.activeInHierarchy)  return ghost;
        }

        GameObject newGhost = Instantiate(ghostPrefab, transform);

        newGhost.SetActive(false);
        pool.Add(newGhost);

        return newGhost;
    }
}