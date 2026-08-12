using System.Collections.Generic;
using UnityEngine;

public class EnemyPooler : MonoBehaviour
{
    public static EnemyPooler Instance;

    [Header("Enemy Prefabs")]
    [SerializeField] private List<GameObject> enemyPrefabs;

    [SerializeField] private int poolSizePerPrefab = 10;

    private readonly List<GameObject> pool = new();


    private void Awake()
    {
        Instance = this;

        foreach (GameObject enemyPrefab in enemyPrefabs)
        {
            for (int i = 0; i < poolSizePerPrefab; i++)
            {
                GameObject obj = Instantiate(enemyPrefab, transform);
            
                obj.SetActive(false);
                pool.Add(obj);
            }
        }
    }


    public GameObject GetEnemy()
    {
        //랜덤 위치부터 탐색해서 적 종류가 한쪽으로 몰리지 않게 함
        int startIndex = UnityEngine.Random.Range(0, pool.Count);

        for (int i = 0; i < pool.Count; i++)
        {
            int index = (startIndex + i) % pool.Count;

            if (!pool[index].activeInHierarchy)
            {
                return pool[index];
            }
        }

        return null;
    }
}