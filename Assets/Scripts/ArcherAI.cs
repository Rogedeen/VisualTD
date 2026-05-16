using UnityEngine;

public class ArcherAI : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private float fireRate = 6f; // Çok daha yavaş (Ok yağmuru anlamlı olsun diye)
    [SerializeField] private float range = 15f;
    [SerializeField] private Transform firePoint;
    // [SerializeField] private float arrowSpawnDelay = 1.2f; // Kaldırıldı -> Artık Animation Event kullanılıyor

    
    [Header("References")]
    [SerializeField] private Animator animator;

    private float nextFireTime;
    private readonly int attackHash = Animator.StringToHash("Attack");
    private EnemyAI currentTarget; // Atış sırasında hedefi hafızada tutmak için
    private Quaternion initialLocalRotation;

    private void Awake()
    {
        initialLocalRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        transform.localRotation = initialLocalRotation;

        // Okçuların aynı anda senkronize ok atmasını önlemek için rastgele bir başlangıç süresi
        nextFireTime = Time.time + Random.Range(1f, fireRate + 2f);
    }

    // Gesture control variables
    public static bool isHoldingFire = false;

    private void Update()
    {
        if (isHoldingFire) return; // Yumruk yapıldıysa ok atma, bekle

        if (Time.time >= nextFireTime)
        {
            EnemyAI target = FindNearestEnemy();
            if (target != null)
            {
                Shoot(target);
            }
        }
    }

    public EnemyAI FindNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        EnemyAI nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (var hit in hits)
        {
            EnemyAI enemy = hit.GetComponent<EnemyAI>();
            // Sadece aktif, hayatta olan ve uyanma (spawn) animasyonu bitmiş düşmanları hedef al!
            if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead && !enemy.IsSpawning)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = enemy;
                }
            }
        }
        return nearest;
    }

    public void Shoot(EnemyAI target)
    {
        if (target == null) return;

        // Bir sonraki atışa rastgelelik ekle ki robot gibi olmasın (Volley atışı sonrası da dahil)
        nextFireTime = Time.time + fireRate + Random.Range(-1.5f, 1.5f);

        // 1. Face the target
        Vector3 lookPosition = new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z);
        transform.LookAt(lookPosition);
        
        // 2. Play Animation
        if (animator != null)
        {
            animator.SetTrigger(attackHash);
        }

        // 3. Hedefi hafızaya al
        currentTarget = target;
        
        // 4. Oku fırlatmak artık sabit saniye ile (Coroutine) değil, 
        // doğrudan Animation Event ile tetiklenecek! (Bkz: ReleaseArrow metodu)
    }

    // Bu metodu Unity Editör'de 'Attack' animasyonunun tam ok çıkış karesine 
    // "Animation Event" olarak eklemelisin.
    public void ReleaseArrow()
    {
        if (currentTarget != null && !currentTarget.IsDead && firePoint != null)
        {
            Vector3 direction = (currentTarget.transform.position - firePoint.position).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);
            
            GameObject arrowObj = ObjectPooler.Instance.SpawnFromPool("Arrow", firePoint.position, rotation);
            if (arrowObj != null)
            {
                Arrow arrowScript = arrowObj.GetComponent<Arrow>();
                if (arrowScript != null)
                {
                    arrowScript.Initialize(currentTarget.transform);
                }
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
