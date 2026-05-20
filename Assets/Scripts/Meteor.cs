using UnityEngine;

public class Meteor : MonoBehaviour
{
    [SerializeField] private float fallSpeed = 20f;
    [SerializeField] private float damageRadius = 3f;
    [SerializeField] private float directDamage = 40f;
    [SerializeField] private GameObject explosionEffectPrefab;

    private Vector3 targetPos;
    private bool hasHit = false;

    public void Launch(Vector3 target)
    {
        targetPos = target;
        hasHit = false;
    }

    private void Update()
    {
        if (hasHit) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, fallSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        hasHit = true;

        // Patlama efekti (Havuzdan veya Instantiate)
        if (ObjectPooler.Instance != null)
            ObjectPooler.Instance.SpawnFromPool("Explosion", transform.position, Quaternion.identity);

        // Alan hasarı
        Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius);
        foreach (var hit in hits)
        {
            EnemyAI enemy = hit.GetComponent<EnemyAI>();
            if (enemy != null) enemy.TakeDamage(directDamage);
        }

        // Objeyi havuza geri gönder (veya inaktif yap)
        gameObject.SetActive(false);
    }
}
