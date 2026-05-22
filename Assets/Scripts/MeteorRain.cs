using UnityEngine;
using System.Collections;

public class MeteorRain : MonoBehaviour
{
    [SerializeField] private float mainDuration = 5f;
    [SerializeField] private float radius = 8f;
    [SerializeField] private float damagePerSec = 40f;

    public void Initialize(Vector3 center)
    {
        // Meteor yağmuru görselinin yere (y=0) denk gelmesi için
        Vector3 floorPos = new Vector3(center.x, 0.1f, center.z);
        transform.position = floorPos;
        StartCoroutine(FullEffectRoutine());
    }

    private IEnumerator FullEffectRoutine()
    {
        // Meteor yağmuru ve yer yanması etkileri hazır prefabın içinde kendiliğinden 
        // başlıyorsa (VFX Graph veya Auto-Play PS), burada sadece hasar süresini sayıyoruz.
        // Tekrar oluşup başlamaması için döngü içinde Spawn çağrısı yapmıyoruz.
        
        float totalDuration = mainDuration;
        float elapsed = 0f;
        
        while (elapsed < totalDuration)
        {
            ApplyAreaDamage(damagePerSec);
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        // Büyü bitti, animasyonları kapat
        if (SkillManager.Instance != null) SkillManager.Instance.EndMeteorAnimation();
        
        if (ObjectPooler.Instance != null)
            ObjectPooler.Instance.ReturnToPool("MeteorRain", gameObject);
        else
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
