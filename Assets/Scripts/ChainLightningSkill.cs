using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ChainLightningSkill : MonoBehaviour
{
    [Header("Settings")]
    public float damage = 40f;
    public float bounceRadius = 5f;
    public int maxBounces = 3;
    public float stunDuration = 1.5f;
    public float delayBetweenBounces = 0.1f;
    public string boltPoolTag = "LightningSegment";

    private HashSet<EnemyAI> hitEnemies = new HashSet<EnemyAI>();

    /// <summary>
    /// Executes the chain lightning starting from a target position and finding the nearest enemy
    /// or starting directly from an enemy.
    /// </summary>
    public void Execute(Vector3 origin, EnemyAI firstTarget = null)
    {
        StartCoroutine(ChainRoutine(origin, firstTarget));
    }

    private IEnumerator ChainRoutine(Vector3 origin, EnemyAI firstTarget)
    {
        hitEnemies.Clear();
        Vector3 lastPos = origin;
        EnemyAI currentTarget = firstTarget;

        if (currentTarget == null)
        {
            currentTarget = FindNearestEnemy(origin, bounceRadius);
        }

        if (currentTarget == null)
        {
            Debug.LogWarning($"[ChainLightningSkill] {origin} konumunda yakinda düsman bulunamadı!");
            yield break;
        }

        int bounces = 0;
        while (currentTarget != null && bounces < maxBounces)
        {
            ApplyEffect(currentTarget);
            hitEnemies.Add(currentTarget);

            // TEK BİR ŞEY YERİNE 2-3 TANE VE DAHA GENİŞ YILDIRIM ÇIKARARAK "YAYILMA" HİSSİ VERELİM
            for (int i = 0; i < 2; i++)
            {
                // Hafif ofsetlerle daha gür bir görünüm
                Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f));
                SpawnBolt(lastPos + offset, currentTarget.transform.position + offset);
            }

            lastPos = currentTarget.transform.position;
            
            yield return new WaitForSeconds(delayBetweenBounces);

            currentTarget = FindNearestEnemy(lastPos, bounceRadius, hitEnemies);
            bounces++;
        }
    }

    private void ApplyEffect(EnemyAI enemy)
    {
        enemy.TakeDamage(damage);
        enemy.ApplyStun(stunDuration);
    }

    private void SpawnBolt(Vector3 start, Vector3 end)
    {
        if (ObjectPooler.Instance == null)
        {
            Debug.LogError("[ChainLightningSkill] ObjectPooler.Instance null!");
            return;
        }

        // Yıldırımın net görünmesi için koordinatları biraz yerden yukarı kaldır (Y ekseni)
        Vector3 raisedStart = start + Vector3.up * 1.5f;
        Vector3 raisedEnd = end + Vector3.up * 1.5f;

        GameObject boltObj = ObjectPooler.Instance.SpawnFromPool(boltPoolTag, Vector3.zero, Quaternion.identity);
        if (boltObj != null)
        {
            LightningBoltSegment segment = boltObj.GetComponent<LightningBoltSegment>();
            if (segment != null)
            {
                segment.Initialize(raisedStart, raisedEnd, 0.3f, boltPoolTag); // Süreyi 0.3'e çıkardık
            }
            else
            {
                Debug.LogError($"[ChainLightningSkill] Prefab üzerinde LightningBoltSegment component'i eksik! Tag: {boltPoolTag}");
            }
        }
        else
        {
            Debug.LogError($"[ChainLightningSkill] Havuzdan '{boltPoolTag}' alınamadı. ObjectPooler ayarlarını kontrol edin.");
        }
    }

    private EnemyAI FindNearestEnemy(Vector3 position, float radius, HashSet<EnemyAI> exclude = null)
    {
        Collider[] hits = Physics.OverlapSphere(position, radius);
        EnemyAI nearest = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            EnemyAI enemy = hit.GetComponent<EnemyAI>();
            if (enemy != null && !enemy.IsDead)
            {
                if (exclude != null && exclude.Contains(enemy)) continue;

                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = enemy;
                }
            }
        }

        return nearest;
    }
}
