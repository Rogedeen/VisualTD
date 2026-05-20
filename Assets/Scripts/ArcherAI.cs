using UnityEngine;

public class ArcherAI : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private float fireRate = 1.5f; 
    [SerializeField] private float range = 15f;
    [SerializeField] private Transform firePoint;
    public float damageMultiplier = 1f;
    
    [Header("References")]
    [SerializeField] private Animator animator;

    private float nextFireTime;
    private readonly int attackHash = Animator.StringToHash("Attack");
    private readonly int dieHash = Animator.StringToHash("Die");
    private readonly int holdHash = Animator.StringToHash("isHolding");
    private EnemyAI currentTarget; // Atış sırasında hedefi hafızada tutmak için
    private bool isDead = false;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }
    }

    private void OnEnable()
    {
        // Okçuların aynı anda senkronize ok atmasını önlemek için rastgele bir başlangıç süresi
        ApplyRandomOffset();
    }

    private void ApplyRandomOffset()
    {
        nextFireTime = Time.time + Random.Range(0f, fireRate);
    }

    // Gesture control variables
    public static bool isHoldingFire = false;

    private bool wasHolding = false;

    private void Update()
    {
        EnemyAI target = FindNearestEnemy();
        
        // Eğer sahnede hedef yoksa okçular bekleme (Idle) pozisyonuna geçsin
        bool shouldHold = (isHoldingFire || target == null);

        if (animator != null)
        {
            animator.SetBool(holdHash, shouldHold);
            if (target == null) animator.ResetTrigger(attackHash); // Hedef yoksa atak animasyonunu temizle
        }

        // Hold bırakıldığında (veya ok yağmuru atılıp bitince) tekrar hepsi aynı anda
        // ateş etmesin diye rastgele bir bekleme (offset) ekliyoruz.
        if (wasHolding && !shouldHold)
        {
            ApplyRandomOffset();
        }
        wasHolding = shouldHold;

        if (isDead || shouldHold) return;

        if (target != null && Time.time >= nextFireTime)
        {
            Shoot(target);
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
        if (target == null || isDead) return;

        // Bir sonraki atışa rastgelelik ekle
        nextFireTime = Time.time + fireRate + Random.Range(-0.2f, 0.2f);

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
    }

    public void ReleaseArrow()
    {
        if (isDead) return;

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

    public void FallAndDie()
    {
        if (isDead) return;
        isDead = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.isKinematic = false;
        rb.useGravity = true;
        
        // Rastgele bir fırlama efekti (sağa/sola/öne/arkaya biraz ivme)
        Vector3 randomTorque = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
        rb.AddTorque(randomTorque, ForceMode.Impulse);
        
        // Hafifçe dışarı doğru ittir
        Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f)).normalized;
        rb.AddForce(randomDir * 5f, ForceMode.Impulse);

        if (animator != null)
        {
            animator.SetTrigger(dieHash);
        }
        
        // Saniye bekle ve sonra kendini kapat (poola dönmesi vs eklenebilir, şimdilik destroy da olabilir)
        // Tower gidince okçu da ölecek.
        Destroy(gameObject, 3f);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
