using UnityEngine;
using System.Collections;

[System.Serializable]
public struct Wave
{
    [Tooltip("Bu dalgada çıkacak toplam düşman sayısı")]
    public int enemyCount;
    [Tooltip("Düşmanların ne kadar sıklıkla çıkacağı")]
    public float spawnRate;
}

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    
    [Header("Wave Settings")]
    [SerializeField] private Wave[] waves;
    [SerializeField] private float timeBetweenWaves = 10f;
    
    private int currentWaveIndex = 0;
    private int spawnPointIndex = 0;

    private void Start()
    {
        if (waves.Length > 0)
        {
            StartCoroutine(SpawnWavesRoutine());
        }
        else
        {
            Debug.LogWarning("Hiç Wave ayarlanmamış! EnemyManager'dan wave ekleyin.");
        }
    }

    private IEnumerator SpawnWavesRoutine()
    {
        yield return new WaitForSeconds(3f); // Initial delay

        while (currentWaveIndex < waves.Length)
        {
            Debug.Log($"Wave {currentWaveIndex + 1} Başladı!");
            Wave currentWave = waves[currentWaveIndex];

            for (int i = 0; i < currentWave.enemyCount; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(currentWave.spawnRate);
            }

            currentWaveIndex++;
            
            if (currentWaveIndex < waves.Length)
            {
                Debug.Log($"Sonraki dalga için bekleniyor... ({timeBetweenWaves} saniye)");
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }
        
        Debug.Log("Tüm dalgalar tamamlandı! (Oyuncu Kazandı)");
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
