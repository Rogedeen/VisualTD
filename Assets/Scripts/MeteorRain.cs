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
            float waitTime = meteorInterval + Random.Range(-0.05f, 0.05f);
            elapsed += waitTime;
            yield return new WaitForSeconds(waitTime);
        }

        // Yağmur bitti, ama yer yanmaya devam ediyor (Ground Burn hala Update'de çalışıyor)
        Debug.Log("Meteor Rain ended, ground still burning...");
        yield return new WaitForSeconds(4f); // 4 saniye daha yanmaya devam et
        
        gameObject.SetActive(false); // Havuza geri dön
    }

    private void SpawnMeteor()
    {
        Vector3 spawnOffset = new Vector3(Random.Range(-radius, radius), 0, Random.Range(-radius, radius));
        Vector3 landPos = transform.position + spawnOffset;
        Vector3 spawnPos = landPos + Vector3.up * 20f;

        GameObject meteorObj = ObjectPooler.Instance.SpawnFromPool("Meteor", spawnPos, Quaternion.Euler(90, 0, 0));
        if (meteorObj != null)
        {
            Meteor meteorScript = meteorObj.GetComponent<Meteor>();
            if (meteorScript != null) meteorScript.Launch(landPos);
        }
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
