using UnityEngine;
using System.Collections;

public class MeteorRain : MonoBehaviour
{
    [SerializeField] private float duration = 5f;
    [SerializeField] private float radius = 8f;
    [SerializeField] private float damagePerMeteor = 30f;
    [SerializeField] private float groundBurnDamagePerSec = 5f;
    [SerializeField] private float meteorInterval = 0.2f;

    public void Initialize(Vector3 center)
    {
        transform.position = center;
        StartCoroutine(RainRoutine());
    }

    private IEnumerator RainRoutine()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            SpawnMeteor();
            elapsed += meteorInterval;
            yield return new WaitForSeconds(meteorInterval);
        }

        // Meteor yağmuru bittikten sonra bir süre daha yanmaya devam etmesi için buraya ek mantık gelebilir 
        // veya prefaba bağlı Particle System bu süreyi yönetebilir.
        Destroy(gameObject, 3f); // Temizlik
    }

    private void SpawnMeteor()
    {
        Vector3 randomPos = transform.position + new Vector3(Random.Range(-radius, radius), 20f, Random.Range(-radius, radius));
        // Meteor objesini havuzdan çıkar
        GameObject meteor = ObjectPooler.Instance.SpawnFromPool("Meteor", randomPos, Quaternion.Euler(90, 0, 0));
        
        // Meteor yere çarptığında alan hasarı vermesi Meteor scriptinin içinde olmalı.
        // Ama biz burada basitçe merkezdeki düşmanlara saniye başı hasar da verebiliriz.
    }

    private void Update()
    {
        // Alan içindeki düşmanlara saniye başı yer yanması (ground burn) hasarı
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            EnemyAI enemy = hit.GetComponent<EnemyAI>();
            if (enemy != null) enemy.TakeDamage(groundBurnDamagePerSec * Time.deltaTime);
        }
    }
}
