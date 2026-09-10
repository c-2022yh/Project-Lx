using System;
using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float spawnRate = 5f;
    private float timer;

    [Header("소환 범위 (맵 좌표 기준)")]
    [SerializeField] private float minX;
    [SerializeField] private float maxX;
    [SerializeField] private float minY;
    [SerializeField] private float maxY;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnRate)
        {
            Spawn();
            timer = 0f;
        }
    }

    void Spawn()
    {
        GameObject enemy = EnemyPooler.Instance.GetEnemy();
        if (enemy != null)
        {
            //설정한 범위 내에서 랜덤 좌표 추출
            float randomX = UnityEngine.Random.Range(minX, maxX);
            float randomY = UnityEngine.Random.Range(minY, maxY);

            enemy.transform.position = new Vector2(randomX, randomY);
            enemy.SetActive(true);
        }
    }
}