using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float maxHealth = 500f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 2f;
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
    private Transform castleTarget;
    private CastleManager castleManager;
    private Vector3 castleNavMeshDestination;
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

    // NavMesh arama yarıçapı — spawn noktaların navmesh'ten ne kadar uzakta
    // olabileceğine göre bunu artır (5 → 10 gibi)
    private const float NAVMESH_SAMPLE_RADIUS = 5f;
    private const float TARGET_SAMPLE_RADIUS = 8f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (healthBar == null) healthBar = GetComponentInChildren<HealthBar>();
        if (damageFlash == null) damageFlash = GetComponent<DamageFlash>();
    }

    private void OnEnable()
    {
        // State sıfırla
        currentState = EnemyState.Spawning;
        currentHealth = maxHealth;
        isDead = false;
        lastAttackTime = -Mathf.Infinity;
        lastTargetValidationTime = Time.time;

        GetComponent<Collider>().enabled = true;

        // Animator sıfırla — önceki triggerlar birikmiş olabilir
        if (animator != null)
        {
            animator.ResetTrigger(attackHash);
            animator.ResetTrigger(dieHash);
            animator.SetFloat(speedHash, 0f);
        }

        // Agent'ı NavMesh'e oturt
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
                // NavMesh bulunamadı — bu objeyi kullanma, geri havuza gönder
                Debug.LogWarning($"[EnemyAI] Spawn noktası NavMesh'ten çok uzak: {transform.position}. " +
                                 $"NAVMESH_SAMPLE_RADIUS={NAVMESH_SAMPLE_RADIUS} artırılabilir ya da spawn noktası taşınmalı.");
                StartCoroutine(ReturnToPoolNextFrame());
                return;
            }
        }

        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);

        if (!FindClosestCastle())
        {
            Debug.LogWarning("[EnemyAI] Kale bulunamadı!");
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

    // Aynı frame'de SetActive(false) çağırmaktan kaçın
    private IEnumerator ReturnToPoolNextFrame()
    {
        yield return null;
        ObjectPooler.Instance.ReturnToPool("Enemy", gameObject);
    }

    private bool FindClosestCastle()
    {
        GameObject[] castles = GameObject.FindGameObjectsWithTag("Castle");
        if (castles.Length == 0) return false;

        float closestDistance = Mathf.Infinity;
        GameObject closestCastle = null;

        foreach (GameObject castle in castles)
        {
            if (castle == null || !castle.activeInHierarchy) continue;

            float distance = Vector3.Distance(transform.position, castle.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCastle = castle;
            }
        }

        if (closestCastle == null) return false;

        castleTarget = closestCastle.transform;
        castleManager = closestCastle.GetComponent<CastleManager>();

        if (!TryUpdateCastleDestination())
        {
            castleTarget = null;
            castleManager = null;
            return false;
        }

        return true;
    }

    private bool TryUpdateCastleDestination()
    {
        if (castleTarget == null || agent == null)
        {
            return false;
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(castleTarget.position, out hit, TARGET_SAMPLE_RADIUS, NavMesh.AllAreas))
        {
            castleNavMeshDestination = hit.position;
            return true;
        }

        castleNavMeshDestination = castleTarget.position;
        return false;
    }

    private IEnumerator SpawnSequence()
    {
        // Spawn animasyonu süresince bekle
        yield return new WaitForSeconds(SPAWN_ANIMATION_DURATION);

        if (isDead || castleTarget == null) yield break;

        // Agent navmesh'te değilse kısa süre bekleyip tekrar dene
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
                Debug.LogWarning($"[EnemyAI] NavMesh'e oturulamadı, geri gönderiliyor: {transform.position}");
                ObjectPooler.Instance.ReturnToPool("Enemy", gameObject);
                yield break;
            }
        }

        currentState = EnemyState.Moving;

        TryUpdateCastleDestination();

        bool success = agent.SetDestination(castleNavMeshDestination);
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

        // Hedef doğrulama
        if (Time.time >= lastTargetValidationTime + targetValidationInterval)
        {
            ValidateTarget();
            lastTargetValidationTime = Time.time;
        }

        // Spawning sırasında animator'ı yine de güncelle (idle/spawn anim oynasın)
        if (currentState == EnemyState.Spawning)
        {
            if (animator != null) animator.SetFloat(speedHash, 0f);
            return;
        }

        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        // Hız animasyonu
        if (animator != null)
            animator.SetFloat(speedHash, agent.velocity.magnitude);

        if (!agent.pathPending && agent.isOnNavMesh)
        {
            float distanceToCastle = Vector3.Distance(transform.position, castleNavMeshDestination);

            if (distanceToCastle <= attackRange)
                TransitionToAttack();
            else
                TransitionToMoving();
        }
    }

    private void ValidateTarget()
    {
        if (castleTarget == null || !castleTarget.gameObject.activeInHierarchy)
        {
            if (!FindClosestCastle())
            {
                Debug.Log("[EnemyAI] Geçerli kale kalmadı, enemy duruyor.");
                if (agent.isOnNavMesh) agent.isStopped = true;
                currentState = EnemyState.Spawning;
            }
            else if (currentState == EnemyState.Moving && agent.isOnNavMesh)
            {
                // Yeni kale bulundu, hedefe güncelle
                TryUpdateCastleDestination();
                agent.SetDestination(castleNavMeshDestination);
            }
        }
    }

    private void TransitionToAttack()
    {
        if (currentState == EnemyState.Attacking) return;

        currentState = EnemyState.Attacking;
        agent.isStopped = true;

        if (Time.time >= lastAttackTime + attackCooldown)
            AttackCastle();
    }

    private void TransitionToMoving()
    {
        if (currentState == EnemyState.Moving) return;

        currentState = EnemyState.Moving;
        agent.isStopped = false;

        if (!agent.hasPath || agent.remainingDistance < 0.1f)
        {
            TryUpdateCastleDestination();
            agent.SetDestination(castleNavMeshDestination);
        }
    }

    private void AttackCastle()
    {
        lastAttackTime = Time.time;
        if (animator != null) animator.SetTrigger(attackHash);

        if (castleManager != null && !castleManager.IsDestroyed)
            castleManager.TakeDamage(attackDamage);
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
