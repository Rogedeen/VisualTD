using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;
    
    // Stats will be loaded from EnemyData
    private float maxHealth;
    private float attackDamage;
    private float attackRange;
    private float attackCooldown;
    
    [SerializeField] private float targetValidationInterval = 1f;

    [Header("UI & Feedback")]
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private DamageFlash damageFlash;

    // State Machine
    private enum EnemyState { Spawning, Moving, Attacking, Dead }
    private EnemyState currentState = EnemyState.Spawning;

    private float currentHealth;
    private NavMeshAgent agent;
    private Animator animator;
    
    private Transform currentTarget;
    private StructureManager targetStructure;
    private Vector3 navMeshDestination;
    
    private float lastAttackTime = -Mathf.Infinity;
    private float lastTargetValidationTime;
    private bool isDead = false;

    // DEBUG: Field'ları dışarıya açıyoruz
    public Transform CurrentTarget => currentTarget;
    public StructureManager TargetStructure => targetStructure;

    public bool IsDead => isDead;
    public bool IsSpawning => currentState == EnemyState.Spawning;

    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int attackHash = Animator.StringToHash("Attack");
    private readonly int dieHash = Animator.StringToHash("Die");

    private Coroutine spawnCoroutine;
    private const float SPAWN_ANIMATION_DURATION = 2f;

    private const float NAVMESH_SAMPLE_RADIUS = 5f;
    private const float TARGET_SAMPLE_RADIUS = 8f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (healthBar == null) healthBar = GetComponentInChildren<HealthBar>();
        if (damageFlash == null) damageFlash = GetComponent<DamageFlash>();
        
        // GAZ-FREN VE TİTREME DÜZELTMESİ:
        if (agent != null)
        {
            agent.acceleration = 1000f; // Anında hızlanma
            agent.autoBraking = false; // Hedefe yaklaşırken yavaşlama yapma
            
            // TİTREME İÇİN KRİTİK: Agent kendi transformunu güncellemeyecek, biz Update'te yapacağız.
            agent.updatePosition = false;
            agent.updateRotation = true; 
        }
    }

    public void SetEnemyData(EnemyData data)
    {
        enemyData = data;
        if (enemyData != null)
        {
            maxHealth = enemyData.maxHealth;
            currentHealth = maxHealth;
            attackDamage = enemyData.attackDamage;
            attackRange = enemyData.attackRange;
            attackCooldown = enemyData.attackCooldown;
            
            if (agent != null)
            {
                agent.speed = enemyData.moveSpeed;
                agent.stoppingDistance = attackRange - 0.5f;
            }

            if (animator != null && enemyData.animatorController != null)
            {
                animator.runtimeAnimatorController = enemyData.animatorController;
            }
        }
    }

    private void OnEnable()
    {
        currentState = EnemyState.Spawning;
        
        // Eğer data atanmışsa değerleri güncelle, yoksa mevcutları kullan
        if (enemyData != null) SetEnemyData(enemyData);
        else currentHealth = maxHealth;

        isDead = false;
        lastAttackTime = -Mathf.Infinity;
        lastTargetValidationTime = Time.time;

        GetComponent<Collider>().enabled = true;

        if (animator != null)
        {
            animator.ResetTrigger(attackHash);
            animator.ResetTrigger(dieHash);
            animator.SetFloat(speedHash, 0f);
        }

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, NAVMESH_SAMPLE_RADIUS, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning($"[EnemyAI] Spawn noktası NavMesh'ten çok uzak: {transform.position}.");
                StartCoroutine(ReturnToPoolNextFrame());
                return;
            }
        }

        // ÖNEMLİ: Eğer pooler henüz objeleri hazırlıyorsa (Awake sırasında instantiate ediyorsa) 
        // hedef arama; çünkü henüz TargetManager veya CoreManager hazır olmayabilir.
        if (gameObject.activeInHierarchy && Time.frameCount > 0)
        {
            if (!FindTarget())
            {
                Debug.LogWarning("[EnemyAI] Hedef bulunamadı!");
                StartCoroutine(ReturnToPoolNextFrame());
                return;
            }

            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(SpawnSequence());
        }
    }

    private void OnDisable()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    private IEnumerator ReturnToPoolNextFrame()
    {
        yield return null;
        ObjectPooler.Instance.ReturnToPool("Enemy", gameObject);
    }

    private bool FindTarget()
    {
        if (TargetManager.Instance == null) return false;

        StructureManager sTarget;
        Transform decision = TargetManager.Instance.GetDecision(transform.position, agent, out sTarget);
        
        if (decision != null)
        {
            currentTarget = decision;
            targetStructure = sTarget;
            TryUpdateDestination();
            return true;
        }

        return false;
    }

    private bool TryUpdateDestination()
    {
        if (currentTarget == null || agent == null) return false;

        // Hedefi debug etmek için log ekleyelim
        // Debug.Log($"[EnemyAI] {name} setting destination to {currentTarget.name} at {currentTarget.position}");

        NavMeshHit hit;
        // Radius'u genişletiyoruz ve daha agresif bir kontrol yapıyoruz
        if (NavMesh.SamplePosition(currentTarget.position, out hit, 10f, NavMesh.AllAreas))
        {
            navMeshDestination = hit.position;
            // TargetManager'dan gelen kule/kapı ise tam noktaya gitmeye çalış
            if (targetStructure != null) navMeshDestination = hit.position;
        }
        else
        {
            navMeshDestination = currentTarget.position;
        }

        // Eğer hedef (-1, 0, 2) gibi garip bir yerse burada yakalayabiliriz
        if (Vector3.Distance(navMeshDestination, Vector3.zero) < 5f && Vector3.Distance(currentTarget.position, Vector3.zero) > 10f)
        {
            Debug.LogWarning($"[EnemyAI] {name} suspicious destination detected: {navMeshDestination} for target {currentTarget.name}");
        }

        return true;
    }

    private IEnumerator SpawnSequence()
    {
        yield return new WaitForSeconds(SPAWN_ANIMATION_DURATION);

        if (isDead || currentTarget == null) yield break;

        if (!agent.isOnNavMesh)
        {
            float elapsed = 0f;
            while (!agent.isOnNavMesh && elapsed < 1f)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, NAVMESH_SAMPLE_RADIUS, NavMesh.AllAreas))
                    agent.Warp(hit.position);

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning($"[EnemyAI] NavMesh'e oturulamadı: {transform.position}");
                ObjectPooler.Instance.ReturnToPool("Enemy", gameObject);
                yield break;
            }
        }

        currentState = EnemyState.Moving;

        TryUpdateDestination();

        bool success = agent.SetDestination(navMeshDestination);
        if (success)
        {
            agent.isStopped = false;
        }
        else
        {
            Debug.LogWarning($"[EnemyAI] SetDestination başarısız: {transform.position}");
            ObjectPooler.Instance.ReturnToPool("Enemy", gameObject);
        }
    }

    private void Update()
    {
        if (isDead) return;

        // NavMesh Agent'ın pozisyonu ile Transform'u manuel senkronize ederek titremeyi engelle
        if (agent != null && agent.isOnNavMesh)
        {
             // Agent bir sonraki karede nerede olacağını biliyor, ona yumuşakça (veya direkt) geç
             Vector3 targetPos = agent.nextPosition;
             transform.position = new Vector3(targetPos.x, targetPos.y, targetPos.z);
             
             // Agent'ın kendi transform güncellemesini kapatalım ki bizimle savaşmasın
             agent.updatePosition = false;
        }
        else
        {
            transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        }
        
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);

        // Hedef validasyonu ve zeka güncellemelerini daha seyrek yapıyoruz (Smooth karar verme için)
        if (Time.time >= lastTargetValidationTime + targetValidationInterval)
        {
            ValidateTarget();
            lastTargetValidationTime = Time.time;
        }

        if (currentState == EnemyState.Spawning)
        {
            if (animator != null) animator.SetFloat(speedHash, 0f);
            return;
        }

        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        // Hedefe bakması için yönlenme (Saldırırken de yararlı)
        if (currentTarget != null)
        {
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
        }

        // ANIMASYON DÜZELTME: Daha kararlı hız kontrolü
        if (animator != null)
        {
            // Agent'ın gerçek hareket hızına bakıyoruz
            float currentVelocity = agent.velocity.magnitude;
            // Eğer hareket ediyorsa (ve durdurulmamışsa) Speed 1, yoksa 0
            float animationSpeed = (currentVelocity > 0.1f && !agent.isStopped) ? 1.0f : 0f; 

            animator.SetFloat(speedHash, animationSpeed);
        }

        if (!agent.pathPending && agent.isOnNavMesh)
        {
            float distanceToTarget = Vector3.Distance(transform.position, navMeshDestination);

            // Daha güvenli bir mesafe kontrolü (Hem AttackRange hem de NavMesh StoppingDistance)
            if (distanceToTarget <= attackRange || agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                TransitionToAttack();
            else
                TransitionToMoving();
        }
    }

    private void ValidateTarget()
    {
        // Smooth Karar Verme: Sadece hedef yoksa veya hedef öldüyse yeni hedef ara.
        // Sürekli yol hesaplamak titreşime (jitter) sebep olur.
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy || (targetStructure != null && targetStructure.IsDestroyed))
        {
            if (FindTarget())
            {
                if (currentState == EnemyState.Moving && agent.isOnNavMesh)
                {
                    agent.SetDestination(navMeshDestination);
                }
            }
            else
            {
                if (agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
            }
        }
    }

    private void TransitionToAttack()
    {
        // Hedefe bakmayı zorla
        if (currentTarget != null)
        {
            Vector3 dir = (currentTarget.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
        }

        if (currentState == EnemyState.Attacking)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
                AttackTarget();
            return;
        }

        currentState = EnemyState.Attacking;
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; // Kaymayı durdur
        }

        if (Time.time >= lastAttackTime + attackCooldown)
            AttackTarget();
    }

    private void TransitionToMoving()
    {
        if (currentState == EnemyState.Moving) return;

        currentState = EnemyState.Moving;
        agent.isStopped = false;

        if (!agent.hasPath || agent.remainingDistance < 0.1f)
        {
            TryUpdateDestination();
            agent.SetDestination(navMeshDestination);
        }
    }

    private void AttackTarget()
    {
        lastAttackTime = Time.time;
        
        // Güvenlik: Hedef hala yaşıyor mu ve menzilde mi?
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            TransitionToMoving();
            return;
        }

        // Atak anında tekrar hedefe bakmayı zorla
        if (currentTarget != null)
        {
            Vector3 dir = (currentTarget.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
        }

        if (animator != null)
        {
            animator.SetTrigger(attackHash);
            // Debug için log ekleyebilirsiniz: Debug.Log($"{name} is attacking {currentTarget.name}");
        }

        // Hedef bir binaysa ona hasar ver
        if (targetStructure != null && !targetStructure.IsDestroyed)
        {
            targetStructure.TakeDamage(attackDamage);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        
        // --- SAĞLIK BARINI GÜNCELLE ---
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }

        if (damageFlash != null) damageFlash.Flash();

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        currentState = EnemyState.Dead;
        isDead = true;
        agent.isStopped = true;
        GetComponent<Collider>().enabled = false;

        // Altın ödülünü ver
        if (enemyData != null && GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(enemyData.goldReward);
        }

        if (animator != null) animator.SetTrigger(dieHash);
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(2f);
        ObjectPooler.Instance.ReturnToPool("Enemy", gameObject);
    }
}
