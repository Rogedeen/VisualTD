using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    [Header("Skill Settings")]
    [SerializeField] private float lightningDamage = 50f;
    [SerializeField] private float healAmount = 200f;
    [SerializeField] private float lightningRadius = 5f;

    [Header("References")]
    [SerializeField] private CastleManager castleManager;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void TriggerArrowVolley(Vector3 targetPosition)
    {
        Debug.Log("Skill Triggered: Arrow Volley at " + targetPosition);
        // Spawn 10 arrows in a radius around the target position
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-2f, 2f), 10f, Random.Range(-2f, 2f));
            Vector3 spawnPos = targetPosition + randomOffset;
            ObjectPooler.Instance.SpawnFromPool("Arrow", spawnPos, Quaternion.Euler(90, 0, 0));
        }
    }

    public void TriggerLightningStrike(Vector3 targetPosition)
    {
        Debug.Log("Skill Triggered: Lightning Strike at " + targetPosition);
        
        // Spawn Visual Effect
        ObjectPooler.Instance.SpawnFromPool("Lightning", targetPosition, Quaternion.identity);

        // Deal Area of Effect (AoE) Damage
        Collider[] hitColliders = Physics.OverlapSphere(targetPosition, lightningRadius);
        foreach (var hitCollider in hitColliders)
        {
            EnemyAI enemy = hitCollider.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(lightningDamage);
            }
        }
    }

    public void TriggerFortifyWall()
    {
        Debug.Log("Skill Triggered: Fortify Wall");
        if (castleManager != null)
        {
            castleManager.HealWall(healAmount);
            // Optional: Spawn Heal particle effect at castle position
            // ObjectPooler.Instance.SpawnFromPool("HealEffect", castleManager.transform.position, Quaternion.identity);
        }
    }
}
