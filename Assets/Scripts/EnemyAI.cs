using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float attackDamage = 5f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 1.5f;
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
        
        // NOTE: Do not override NavMeshAgent.speed here so prefab/inspector values are respected.
    }

    private void OnEnable()
    {
        currentState = EnemyState.Spawning;
        currentHealth = maxHealth;
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

        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);

        if (!FindTarget())
        {
            Debug.LogWarning("[EnemyAI] Hedef bulunamadı!");
            StartCoroutine(ReturnToPoolNextFrame());
            return;
        }

        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnSequence());
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
        StructureManager[] structures = FindObjectsOfType<StructureManager>();
        StructureManager gate = null;
        
        // Önce Kapıyı (Gate) bul
        foreach (var s in structures)
        {
            if (s.type == StructureType.Gate && !s.IsDestroyed && s.gameObject.activeInHierarchy)
            {
                gate = s;
                break;
            }
        }

        if (gate != null)
        {
            currentTarget = gate.transform;
            targetStructure = gate;
            
            // Kapıya yol var mı bak?
            TryUpdateDestination();
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(navMeshDestination, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                // Mevcut yol tam, kapıya git
                return true;
            }
            
            // Kapıya yol yoksa (duvarlar kapalıysa), en yakın Duvara (Wall) yönel
            StructureManager closestWall = null;
            float minDist = Mathf.Infinity;
            foreach (var s in structures)
            {
                if (s.type == StructureType.Wall && !s.IsDestroyed && s.gameObject.activeInHierarchy)
                {
                    float d = Vector3.Distance(transform.position, s.transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closestWall = s;
                    }
                }
            }

            if (closestWall != null)
            {
                currentTarget = closestWall.transform;
                targetStructure = closestWall;
                TryUpdateDestination();
                return true;
            }
        }
        
        // Kapı yoksa veya yıkıldıysa (veya kapıya giden yol/duvar kalmadıysa) İçeriye (Core) git
        if (CoreManager.Instance != null)
        {
            currentTarget = CoreManager.Instance.transform;
            targetStructure = null;
            TryUpdateDestination();
            return true;
        }

        return false;
    }

    private bool TryUpdateDestination()
    {
        if (currentTarget == null || agent == null) return false;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(currentTarget.position, out hit, TARGET_SAMPLE_RADIUS, NavMesh.AllAreas))
        {
            navMeshDestination = hit.position;
            return true;
        }

        navMeshDestination = currentTarget.position;
        return false;
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

        if (animator != null)
            animator.SetFloat(speedHash, agent.velocity.magnitude);

        if (!agent.pathPending && agent.isOnNavMesh)
        {
            float distanceToTarget = Vector3.Distance(transform.position, navMeshDestination);

            if (distanceToTarget <= attackRange)
                TransitionToAttack();
            else
                TransitionToMoving();
        }
    }

    private void ValidateTarget()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy || (targetStructure != null && targetStructure.IsDestroyed))
        {
            if (!FindTarget())
            {
                if (agent.isOnNavMesh) agent.isStopped = true;
                currentState = EnemyState.Spawning; // Durum korumak için geçici
            }
            else if (currentState == EnemyState.Moving && agent.isOnNavMesh)
            {
                TryUpdateDestination();
                agent.SetDestination(navMeshDestination);
            }
        }
        else if (agent.isOnNavMesh)
        {
            // Eğer hedefe giden yol tıkalıysa (duvar varsa) en yakın duvara saldır
            if (agent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                StructureManager[] structures = FindObjectsOfType<StructureManager>();
                StructureManager closestWall = null;
                float minDist = Mathf.Infinity;
                foreach (var s in structures)
                {
                    if (s.type == StructureType.Wall && !s.IsDestroyed && s.gameObject.activeInHierarchy)
                    {
                        float d = Vector3.Distance(transform.position, s.transform.position);
                        if (d < minDist)
                        {
                            minDist = d;
                            closestWall = s;
                        }
                    }
                }

                if (closestWall != null)
                {
                    currentTarget = closestWall.transform;
                    targetStructure = closestWall;
                    TryUpdateDestination();
                    agent.SetDestination(navMeshDestination);
                }
            }
        }
    }

    private void TransitionToAttack()
    {
        if (currentState == EnemyState.Attacking) return;

        currentState = EnemyState.Attacking;
        agent.isStopped = true;

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
        if (animator != null) animator.SetTrigger(attackHash);

        // Hedef bir binaysa ona hasar ver
        if (targetStructure != null && !targetStructure.IsDestroyed)
        {
            targetStructure.TakeDamage(attackDamage);
        }
        else if (currentTarget == CoreManager.Instance?.transform)
        {
            // Eğer hedefe (Core) ulaştıysak aslında trigger içine girmesi beklenir, ama range'den vuruyorsa diye
            // Collider'ın OnTriggerEnter kısmı halledecek.
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);
        if (damageFlash != null) damageFlash.Flash();

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        currentState = EnemyState.Dead;
        isDead = true;
        agent.isStopped = true;
        GetComponent<Collider>().enabled = false;
        if (animator != null) animator.SetTrigger(dieHash);
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(2f);
        ObjectPooler.Instance.ReturnToPool("Enemy", gameObject);
    }
}
