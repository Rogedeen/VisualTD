using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    [Header("Skill Settings")]
    [SerializeField] private float lightningDamage = 50f;
    [SerializeField] private float healAmount = 200f;
    [SerializeField] private float lightningRadius = 5f;

    [Header("References")]
    [SerializeField] private StructureManager mainGate; // The main gate
    [Tooltip("The Commander character standing on the tower")]
    [SerializeField] private Animator commanderAnimator;

    // Animator Hashes for Commander
    private readonly int castArrowHash = Animator.StringToHash("CastArrow");
    private readonly int castLightningHash = Animator.StringToHash("CastLightning");
    private readonly int castHealHash = Animator.StringToHash("CastHeal");

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void HoldArchers()
    {
        Debug.Log("Skill Triggered: Hold Archers");
        ArcherAI.isHoldingFire = true;
    }

    public void TriggerArrowVolley()
    {
        Debug.Log("Skill Triggered: Arrow Volley!");
        ArcherAI.isHoldingFire = false; // Ateş serbest!

        if (commanderAnimator != null) commanderAnimator.SetTrigger(castArrowHash);

        // Sahnedeki düşmanlara gökyüzünden ok yağdırma efekti
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
        if (enemies.Length > 0)
        {
            for (int i = 0; i < 20; i++)
            {
                // Rastgele bir düşman seç
                EnemyAI randomEnemy = enemies[Random.Range(0, enemies.Length)];
                if (randomEnemy != null && !randomEnemy.IsDead)
                {
                    // Düşmanın biraz tepesinden spawn et (Gökyüzünden yağıyor hissi için)
                    Vector3 spawnPos = randomEnemy.transform.position + new Vector3(Random.Range(-3f, 3f), Random.Range(10f, 15f), Random.Range(-3f, 3f));
                    GameObject arrowObj = ObjectPooler.Instance.SpawnFromPool("Arrow", spawnPos, Quaternion.Euler(90, 0, 0));
                    
                    if (arrowObj != null)
                    {
                        Arrow arrowScript = arrowObj.GetComponent<Arrow>();
                        if (arrowScript != null)
                        {
                            arrowScript.Initialize(randomEnemy.transform);
                        }
                    }
                }
            }
        }
    }

    public void TriggerLightningStrike(Vector3 targetPosition)
    {
        Debug.Log("Skill Triggered: Lightning Strike at " + targetPosition);
        
        if (commanderAnimator != null) commanderAnimator.SetTrigger(castLightningHash);
        
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

        if (commanderAnimator != null) commanderAnimator.SetTrigger(castHealHash);

        if (mainGate != null)
        {
            mainGate.HealWall(healAmount);
            // Optional: Spawn Heal particle effect at castle position
            // ObjectPooler.Instance.SpawnFromPool("HealEffect", mainGate.transform.position, Quaternion.identity);
        }
        else
        {
            // Eğer ana kapı atanmadıysa, sahnede hayatta olan tüm duvarları/kapıları bulup can ver
            StructureManager[] structures = FindObjectsOfType<StructureManager>();
            foreach (var structure in structures)
            {
                structure.HealWall(healAmount);
            }
        }
    }

    public void TriggerMageCast()
    {
        Debug.Log("Skill Triggered: Spiderman Web (Yavaşlatma vb. eklenebilir)");
        // Şimdilik sadece log atıyor, Spiderman için ilerde yavaşlatma eklenebilir.
    }

    public void TriggerFireball()
    {
        Debug.Log("Skill Triggered: Fireball (Adukhet)!");
        
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
        if (enemies.Length > 0)
        {
            // En kalabalık düşman grubunu bulmak için basitçe ilk düşmanı seçip etrafındakileri buluyoruz.
            // İlerde daha gelişmiş bir merkez bulma algoritması yazılabilir.
            EnemyAI target = enemies[Random.Range(0, enemies.Length)];
            if (target != null && !target.IsDead)
            {
                ObjectPooler.Instance.SpawnFromPool("Fireball", target.transform.position, Quaternion.identity);
                
                // Alan hasarı
                float splashRadius = 5f;
                float splashDamage = 150f;
                
                Collider[] colliders = Physics.OverlapSphere(target.transform.position, splashRadius);
                foreach (var col in colliders)
                {
                    EnemyAI enemy = col.GetComponent<EnemyAI>();
                    if (enemy != null && !enemy.IsDead)
                    {
                        enemy.TakeDamage(splashDamage);
                    }
                }
            }
        }
    }

}
