using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private float speed = 8f; // Yavaşlatıldı (Eskiden 20 idi)
    [SerializeField] private float damage = 25f;
    [SerializeField] private float arcHeight = 5f; // Kingdom Rush style kavis

    public Vector3 startPos;
    public Transform target;
    public Vector3 targetPos;
    public float progress;
    public TrailRenderer trail;

    private void Awake()
    {
        trail = GetComponentInChildren<TrailRenderer>();
        SetupTrail();
    }

    private void SetupTrail()
    {
        if (trail == null) return;

        // Daha ince ve zarif rüzgar/hava efekti
        trail.startWidth = 0.08f;  // Önceki 0.05'ten biraz daha belirgin ama hala ince
        trail.endWidth = 0.01f;
        trail.time = 0.25f;        // İz süresi
        trail.minVertexDistance = 0.1f;
        
        // Açık gri, saydam rüzgar rengi
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(0.9f, 0.9f, 0.95f), 0.0f), 
                new GradientColorKey(new Color(0.7f, 0.7f, 0.8f), 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.3f, 0.0f), // %30 saydamlık
                new GradientAlphaKey(0.0f, 1.0f)  // Sona doğru tamamen kaybol
            }
        );
        trail.colorGradient = gradient;

        // Default materyal genelde mor olduğu için, eğer atanmış bir materyal yoksa 
        // Unity'nin standart şeffaf materyalini kullanmaya çalışalım
        if (trail.material == null || trail.material.name.Contains("Default"))
        {
            trail.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    public void Initialize(Transform enemyTarget)
    {
        target = enemyTarget;
        startPos = transform.position;
        progress = 0f;

        if (trail != null) trail.Clear();

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
