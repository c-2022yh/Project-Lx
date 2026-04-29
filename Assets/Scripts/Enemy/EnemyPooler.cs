using System.Collections.Generic;
using UnityEngine;

public class EnemyPooler : MonoBehaviour
{
    public static EnemyPooler Instance;

    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int poolSize = 10;
    private List<GameObject> pool = new List<GameObject>();

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(enemyPrefab);

            //매니저 자식으로 넣어 정리
            obj.transform.SetParent(this.transform); 
            obj.SetActive(false);
            pool.Add(obj);
        }
    }

    public GameObject GetEnemy()
    {
        foreach (var enemy in pool)
        {
            if (!enemy.activeInHierarchy) return enemy;
        }

        return null;
    }
}