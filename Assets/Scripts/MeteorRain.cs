using UnityEngine;
using System.Collections;

public class MeteorRain : MonoBehaviour
{
    [SerializeField] private float mainDuration = 5f;
    [SerializeField] private float burnDuration = 4f;
    [SerializeField] private float radius = 8f;
    [SerializeField] private float damagePerSec = 40f;
    [SerializeField] private string groundBurnVFX = "GroundBurnEffect";

    public void Initialize(Vector3 center)
    {
        transform.position = center;
        StartCoroutine(FullEffectRoutine());
    }

    private IEnumerator FullEffectRoutine()
    {
        // 1. Meteor Yağmuru Aşaması (Hazır Asset zaten prefabın içinde çalışıyor olmalı)
        float elapsed = 0f;
        while (elapsed < mainDuration)
        {
            ApplyAreaDamage(damagePerSec);
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        // 2. Yer Yanması Aşaması (Meteorlar bitti, yer alevli kalıyor)
        // Eğer yer yanması için ayrı bir prefab varsa onu burada açabiliriz
        GameObject burnEffect = ObjectPooler.Instance.SpawnFromPool(groundBurnVFX, transform.position, Quaternion.identity);
        
        elapsed = 0f;
        while (elapsed < burnDuration)
        {
            ApplyAreaDamage(damagePerSec * 0.5f); // Yanma hasarı biraz daha az olabilir
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        if (burnEffect != null) burnEffect.SetActive(false);
        gameObject.SetActive(false);
    }

    private void ApplyAreaDamage(float amount)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            EnemyAI enemy = hit.GetComponent<EnemyAI>();
            if (enemy != null) enemy.TakeDamage(amount * 0.5f); // 0.5s aralıkla vurduğumuz için yarısını veriyoruz
        }
    }
}
