using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float maxHealth = 500f; // Can arttırıldı (Önceden 100'dü)
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 2f;
    
    [Header("UI & Feedback")]
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private DamageFlash damageFlash;
    
    private float currentHealth;
    private NavMeshAgent agent;
    private Animator animator;
    private Transform castleTarget;
    private CastleManager castleManager;
    private float lastAttackTime;
    private Quaternion initialLocalRotation;
    
    private bool isDead = false;
    public bool IsDead => isDead; // Okçular ölüleri hedef almasın diye public yaptık
    
    public bool IsSpawning { get; private set; } = true; // Spawn olurken okçular saldırmasın diye

    // Animator Hashes
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int attackHash = Animator.StringToHash("Attack");
    private readonly int dieHash = Animator.StringToHash("Die");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        initialLocalRotation = transform.localRotation;
        
        if (healthBar == null) healthBar = GetComponentInChildren<HealthBar>();
        if (damageFlash == null) damageFlash = GetComponent<DamageFlash>();
    }

    private void OnEnable()
    {
        IsSpawning = true; // Uyanma modunda
        currentHealth = maxHealth;
        isDead = false;
        transform.localRotation = initialLocalRotation;
        GetComponent<Collider>().enabled = true;

        // Havuzdan çıktığında NavMesh hatalarını önlemek için bulunduğu yere zorla oturt
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.Warp(transform.position);
        }
        
        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);

        // Find the closest Castle/Wall/Tower piece
        GameObject[] castles = GameObject.FindGameObjectsWithTag("Castle");
        float closestDistance = Mathf.Infinity;
        GameObject closestCastle = null;

        foreach (GameObject castle in castles)
        {
            float distance = Vector3.Distance(transform.position, castle.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCastle = castle;
            }
        }

        if (closestCastle != null)
        {
            castleTarget = closestCastle.transform;
            castleManager = closestCastle.GetComponent<CastleManager>();
            // SetDestination'ı buradan kaldırdık, WaitAndMove içine aldık
            agent.isStopped = true; // Spawn animasyonu bitene kadar dur
            StartCoroutine(WaitAndMove());
        }
        else
        {
            // Stop agent if no target is found
            agent.isStopped = true;
        }
    }

    private IEnumerator WaitAndMove()
    {
        yield return new WaitForSeconds(2f); // Spawn animasyonu süresi (yaklaşık)
        IsSpawning = false; // Artık uyanma bitti, okçular saldırabilir

        if (!isDead && castleTarget != null)
        {
            // Rotayı uyanma bittikten sonra belirliyoruz ki NavMesh sorunsuz hesaplasın
            agent.SetDestination(castleTarget.position);
            agent.isStopped = false;
        }
    }

    private void Update()
    {
        // Spawning (uyanma) aşamasındayken Update döngüsü çalışmasın.
        // Aksi halde aşağıdaki isStopped = false komutu uyanma süresini hemen iptal eder.
        if (IsSpawning || isDead || castleTarget == null) return;

        // Update Animator Speed
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            animator.SetFloat(speedHash, agent.velocity.magnitude);
        }

        // Check Attack Range
        float distanceToCastle = Vector3.Distance(transform.position, castleTarget.position);
        if (distanceToCastle <= attackRange)
        {
            agent.isStopped = true;
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackCastle();
            }
        }
        else
        {
            agent.isStopped = false;
        }
    }

    private void AttackCastle()
    {
        lastAttackTime = Time.time;
        animator.SetTrigger(attackHash);
        
        if (castleManager != null)
        {
            castleManager.TakeDamage(attackDamage);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        
        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);
        if (damageFlash != null) damageFlash.Flash();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        agent.isStopped = true;
        GetComponent<Collider>().enabled = false;
        animator.SetTrigger(dieHash);
        
        // Return to pool after animation (Assume death animation takes ~2 seconds)
        StartCoroutine(DeactivateAfterDelay(2f));
    }

    private IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false); // ObjectPooler will reuse this
    }
}
