using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private float speed = 8f; // Yavaşlatıldı (Eskiden 20 idi)
    [SerializeField] private float damage = 25f;
    [SerializeField] private float arcHeight = 5f; // Kingdom Rush style kavis

    private Vector3 startPos;
    private Transform target;
    private Vector3 targetPos;
    private float progress;

    public void Initialize(Transform enemyTarget)
    {
        target = enemyTarget;
        startPos = transform.position;
        progress = 0f;

        if (target != null)
        {
            targetPos = target.position;
        }
    }

    private void Update()
    {
        // Hedef hareket ediyorsa pozisyonunu güncelle, öldüyse son bilinen yere git
        if (target != null && target.gameObject.activeInHierarchy)
        {
            targetPos = target.position;
        }

        // Kavisli hareket hesaplama (Parabol)
        progress += Time.deltaTime * speed / Vector3.Distance(startPos, targetPos);
        
        // Lerp ile x ve z ekseninde düz git
        Vector3 currentPos = Vector3.Lerp(startPos, targetPos, progress);
        
        // y eksenine kavis (parabol) ekle
        float heightOffset = Mathf.Sin(progress * Mathf.PI) * arcHeight;
        currentPos.y += heightOffset;

        // Okun ucunun gideceği yere bakmasını sağla
        Vector3 moveDirection = currentPos - transform.position;
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        transform.position = currentPos;

        // Hedefe ulaştıysa
        if (progress >= 1f)
        {
            HitTarget();
        }
    }

    private void HitTarget()
{
    // Hedefe hasar ver
    if (target != null && target.gameObject.activeInHierarchy)
    {
        EnemyAI enemy = target.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }
    }

    // Oku GERÇEKTEN Object Pooler'a geri gönder
    ObjectPooler.Instance.ReturnToPool("Arrow", gameObject); 
}
}
