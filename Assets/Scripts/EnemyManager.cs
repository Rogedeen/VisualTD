using UnityEngine;
using System.Collections;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int enemiesPerWave = 3;
    [SerializeField] private float waveInterval = 5f;
    [SerializeField] private float enemySpawnDelay = 0.5f;
    
    private float nextWaveTime;
    private int spawnPointIndex = 0;

    private void Update()
    {
        if (Time.time >= nextWaveTime)
        {
            StartCoroutine(SpawnWave());
            nextWaveTime = Time.time + waveInterval;
        }
    }

    private IEnumerator SpawnWave()
    {
        for (int i = 0; i < enemiesPerWave; i++)
        {
            SpawnEnemy();
            
            if (i < enemiesPerWave - 1)
            {
                yield return new WaitForSeconds(enemySpawnDelay);
            }
        }
    }

    private void SpawnEnemy()
    {
        if (spawnPoints.Length == 0) return;

        // Cycle through spawn points instead of random
        Transform spawnPoint = spawnPoints[spawnPointIndex];
        spawnPointIndex = (spawnPointIndex + 1) % spawnPoints.Length;

        ObjectPooler.Instance.SpawnFromPool("Enemy", spawnPoint.position, spawnPoint.rotation);
    }
}
