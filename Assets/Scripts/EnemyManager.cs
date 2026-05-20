using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct EnemySpawnConfig
{
    public EnemyData data;
    [Tooltip("Hangi dalgadan itibaren bu düşman çıkabilir?")]
    public int startWave;
    [Tooltip("Düşmanın çıkma ağırlığı (Weight). Yüksek değer = Daha sık.")]
    public float baseWeight;
    [Tooltip("Düşmanın ağırlığı her wave'de ne kadar artsın?")]
    public float weightIncreasePerWave;
}

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    
    [Header("Procedural Wave Settings")]
    [SerializeField] private EnemySpawnConfig[] enemyPool;
    [SerializeField] private int baseEnemyCount = 10;
    [SerializeField] private int enemiesPerWaveIncrease = 3;
    [SerializeField] private float baseSpawnRate = 1.5f;
    [SerializeField] private float minSpawnRate = 0.5f;

    [Header("Scaling Settings")]
    [SerializeField] private float healthMultiplierPerThreeWaves = 1.2f;
    
    private int currentWaveIndex = 0;
    private int spawnPointIndex = 0;

    private void Start()
    {
        if (enemyPool != null && enemyPool.Length > 0)
        {
            StartCoroutine(SpawnWavesRoutine());
        }
        else
        {
            Debug.LogWarning("Enemy Pool boş! Lütfen düşman tiplerini ekleyin.");
        }
    }

    private IEnumerator SpawnWavesRoutine()
    {
        yield return new WaitForSeconds(3f);

        while (true) // Sonsuz mod veya belirli bir şart
        {
            Debug.Log($"<color=cyan>Wave {currentWaveIndex + 1} Başladı!</color>");
            
            int totalEnemiesToSpawn = baseEnemyCount + (currentWaveIndex * enemiesPerWaveIncrease);
            float currentSpawnRate = Mathf.Max(minSpawnRate, baseSpawnRate - (currentWaveIndex * 0.05f));

            for (int i = 0; i < totalEnemiesToSpawn; i++)
            {
                EnemyData selectedEnemy = SelectEnemyByWeights();
                if (selectedEnemy != null)
                {
                    SpawnEnemy(selectedEnemy);
                }
                yield return new WaitForSeconds(currentSpawnRate);
            }

            currentWaveIndex++;

            Debug.Log($"Wave bitti. Sonraki dalga için bekleniyor...");
            yield return new WaitUntil(() => Object.FindAnyObjectByType<EnemyAI>() == null);
            yield return new WaitForSeconds(5f);
        }
    }

    private EnemyData SelectEnemyByWeights()
    {
        float totalWeight = 0;
        List<EnemySpawnConfig> availablePool = new List<EnemySpawnConfig>();

        // Mevcut wave için uygun olan düşmanları ve ağırlıklarını hesapla
        foreach (var config in enemyPool)
        {
            if (currentWaveIndex + 1 >= config.startWave)
            {
                float currentWeight = config.baseWeight + (currentWaveIndex * config.weightIncreasePerWave);
                totalWeight += currentWeight;
                availablePool.Add(config);
            }
        }

        if (availablePool.Count == 0) return null;

        // Weighted Random Selection
        float randomValue = Random.Range(0, totalWeight);
        float cursor = 0;

        foreach (var config in availablePool)
        {
            float currentWeight = config.baseWeight + (currentWaveIndex * config.weightIncreasePerWave);
            cursor += currentWeight;
            if (randomValue <= cursor) return config.data;
        }

        return availablePool[0].data;
    }

    private void SpawnEnemy(EnemyData data)
    {
        if (spawnPoints.Length == 0) return;

        Transform spawnPoint = spawnPoints[spawnPointIndex];
        spawnPointIndex = (spawnPointIndex + 1) % spawnPoints.Length;

        GameObject enemyObj = ObjectPooler.Instance.SpawnFromPool("Enemy", spawnPoint.position, spawnPoint.rotation);
        
        if (enemyObj != null)
        {
            EnemyAI enemyAI = enemyObj.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.SetEnemyData(data);
                
                // Her 3 dalgada bir can artışı
                if (currentWaveIndex > 0 && (currentWaveIndex % 3 == 0))
                {
                    float multiplier = Mathf.Pow(healthMultiplierPerThreeWaves, currentWaveIndex / 3);
                    // İlerde EnemyAI içine stats scale metodu eklenebilir
                }
            }
        }
    }
}
