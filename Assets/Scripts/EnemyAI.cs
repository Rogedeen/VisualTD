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
    private float lastAttackTime = -Mathf.Infinity;
    private float lastTargetValidationTime;
    private bool isDead = false;
    
    public bool IsDead => isDead;
    public bool IsSpawning => currentState == EnemyState.Spawning;

    // Animator Hashes
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int attackHash = Animator.StringToHash("Attack");
    private readonly int dieHash = Animator.StringToHash("Die");
    
    // Pooling reset flag
    private Coroutine spawnCoroutine;
    private const float SPAWN_ANIMATION_DURATION = 2f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        if (healthBar == null) healthBar = GetComponentInChildren<HealthBar>();
        if (damageFlash == null) damageFlash = GetComponent<DamageFlash>();
    }

    private void OnEnable()
    {
        // Reset state for pooled object reuse
        currentState = EnemyState.Spawning;
        currentHealth = maxHealth;
        isDead = false;
        lastAttackTime = -Mathf.Infinity;
        lastTargetValidationTime = Time.time;
        
        GetComponent<Collider>().enabled = true;

        // Ensure agent is reset
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.ResetPath();
            agent.Warp(transform.position);
            agent.isStopped = true;
        }
        
        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);

        // Find closest castle
        if (!FindClosestCastle())
        {
            Debug.LogWarning("No castle target found for enemy!");
            agent.isStopped = true;
            return;
        }

        // Start spawn animation sequence
        StopCoroutine(nameof(SpawnSequence));
        spawnCoroutine = StartCoroutine(SpawnSequence());
    }

    private void OnDisable()
    {
        // Critical: Stop all coroutines to prevent stalling
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        
        // Stop NavMesh agent
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    private bool FindClosestCastle()
    {
        GameObject[] castles = GameObject.FindGameObjectsWithTag("Castle");
        if (castles.Length == 0) return false;

        float closestDistance = Mathf.Infinity;
        GameObject closestCastle = null;

        foreach (GameObject castle in castles)
        {
            // Skip destroyed/inactive castles
            if (castle == null || !castle.activeInHierarchy) continue;

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
            return true;
        }

        castleTarget = null;
        castleManager = null;
        return false;
    }

    private IEnumerator SpawnSequence()
    {
        yield return new WaitForSeconds(SPAWN_ANIMATION_DURATION);

        if (isDead || castleTarget == null)
            yield break;

        // Transition to Moving state
        currentState = EnemyState.Moving;

        // Attempt to set destination
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            bool success = agent.SetDestination(castleTarget.position);
            
            if (success)
            {
                agent.isStopped = false;
            }
            else
            {
                Debug.LogWarning($"SetDestination failed for enemy at {transform.position}");
                agent.isStopped = true;
                currentState = EnemyState.Spawning;
            }
        }
    }

    private void Update()
    {
        // No updates while dead
        if (isDead) return;

        // Validate target periodically
        if (Time.time >= lastTargetValidationTime + targetValidationInterval)
        {
            ValidateTarget();
            lastTargetValidationTime = Time.time;
        }

        // No movement/attack during spawn
        if (currentState == EnemyState.Spawning)
        {
            return;
        }

        // Validate agent is valid
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return;

        // Update animator speed
        animator.SetFloat(speedHash, agent.velocity.magnitude);

        // Check if reached destination
        if (!agent.pathPending && agent.hasPath)
        {
            float distanceToCastle = Vector3.Distance(transform.position, castleTarget.position);

            if (distanceToCastle <= attackRange)
            {
                TransitionToAttack();
            }
            else
            {
                TransitionToMoving();
            }
        }
    }

    private void ValidateTarget()
    {
        // Check if current target is still valid
        if (castleTarget == null || !castleTarget.gameObject.activeInHierarchy)
        {
            // Current target destroyed, search for new one
            if (!FindClosestCastle())
            {
                Debug.Log("No valid castle target found. Enemy stopping.");
                agent.isStopped = true;
                currentState = EnemyState.Spawning;
            }
        }
    }

    private void TransitionToAttack()
    {
        if (currentState == EnemyState.Attacking)
            return; // Already attacking

        currentState = EnemyState.Attacking;
        agent.isStopped = true;

        // Try to attack if cooldown elapsed
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            AttackCastle();
        }
    }

    private void TransitionToMoving()
    {
        if (currentState == EnemyState.Moving)
            return; // Already moving

        currentState = EnemyState.Moving;
        agent.isStopped = false;
        
        // Re-validate path if needed
        if (!agent.hasPath || agent.remainingDistance < 0.1f)
        {
            agent.SetDestination(castleTarget.position);
        }
    }

    private void AttackCastle()
    {
        lastAttackTime = Time.time;
        animator.SetTrigger(attackHash);
        
        if (castleManager != null && !castleManager.IsDestroyed)
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
        currentState = EnemyState.Dead;
        isDead = true;
        agent.isStopped = true;
        GetComponent<Collider>().enabled = false;
        animator.SetTrigger(dieHash);
        
        // Return to pool after death animation
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(2f);
        gameObject.SetActive(false);
    }
}
