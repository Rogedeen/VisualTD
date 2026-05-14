using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 2f;
    
    private float currentHealth;
    private NavMeshAgent agent;
    private Animator animator;
    private Transform castleTarget;
    private CastleManager castleManager;
    private float lastAttackTime;
    private bool isDead = false;

    // Animator Hashes
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int attackHash = Animator.StringToHash("Attack");
    private readonly int dieHash = Animator.StringToHash("Die");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        isDead = false;
        GetComponent<Collider>().enabled = true;
        
        // Find Castle if not assigned (Usually better to pass this via EnemyManager)
        if (castleTarget == null)
        {
            GameObject castle = GameObject.FindGameObjectWithTag("Castle");
            if (castle != null)
            {
                castleTarget = castle.transform;
                castleManager = castle.GetComponent<CastleManager>();
            }
        }

        if (castleTarget != null)
        {
            agent.SetDestination(castleTarget.position);
            agent.isStopped = false;
        }
    }

    private void Update()
    {
        if (isDead || castleTarget == null) return;

        // Update Animator Speed
        animator.SetFloat(speedHash, agent.velocity.magnitude);

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
