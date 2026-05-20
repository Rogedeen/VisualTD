using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    [Header("Skill Settings")]
    [SerializeField] private float lightningDamage = 50f;
    [SerializeField] private float healAmount = 200f;
    [SerializeField] private float lightningRadius = 5f;

    [Header("Meteor Settings")]
    [SerializeField] private GameObject meteorRainPrefab;
    [SerializeField] private float meteorDuration = 5f;

    [Header("Cooldowns")]
    [SerializeField] private float arrowVolleyCD = 10f;
    [SerializeField] private float lightningCD = 15f;
    [SerializeField] private float fortifyCD = 20f;
    [SerializeField] private float meteorRainCD = 12f;

    private float lastArrowTime = -100f;
    private float lastLightningTime = -100f;
    private float lastFortifyTime = -100f;
    private float lastMeteorTime = -100f;

    [Header("References")]
    [SerializeField] private StructureManager mainGate; // The main gate
    [Tooltip("The Commander character standing on the tower")]
    [SerializeField] private Animator commanderAnimator;

    private readonly int castLightningHash = Animator.StringToHash("CastLightning");

    [Header("Fortify Balance")]
    [SerializeField] private float fortifyHoldRequired = 8.0f;
    private float fortifyTimer = 0f;
    public bool IsFortifying { get; private set; }

    // Animator Hashes for Commander & Mages
    private readonly int isHoldingHash = Animator.StringToHash("isHolding");
    private readonly int isFortifyingHash = Animator.StringToHash("isFortifying");
    private readonly int fireballHash = Animator.StringToHash("fireball");
    private readonly int attackHash = Animator.StringToHash("attack");
    
    // Legacy / Other
    private readonly int castArrowHash = Animator.StringToHash("CastArrow");

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // --- COMMANDER CONTROL SKILLS ---

    public void HoldArchers(bool isHolding)
    {
        Debug.Log("Skill Context: " + (isHolding ? "HOLDING ARCHERS" : "RELEASING ARCHERS"));
        ArcherAI.isHoldingFire = isHolding;
        
        if (commanderAnimator != null)
        {
            commanderAnimator.SetBool(isHoldingHash, isHolding);
            if (!isHolding) 
            {
                commanderAnimator.SetTrigger(castArrowHash); // Bırakırken CHAAARGE/Shoot hareketi
                TriggerExtraArrowVolley(10); // Bırakınca fazladan 10 ok
            }
        }
    }

    private void TriggerExtraArrowVolley(int count)
    {
        EnemyAI[] enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude);
        if (enemies.Length == 0) return;

        for (int i = 0; i < count; i++)
        {
            EnemyAI randomEnemy = enemies[Random.Range(0, enemies.Length)];
            if (randomEnemy != null && !randomEnemy.IsDead)
            {
                Vector3 spawnPos = randomEnemy.transform.position + new Vector3(Random.Range(-2f, 2f), 12f, Random.Range(-2f, 2f));
                GameObject arrowObj = ObjectPooler.Instance.SpawnFromPool("Arrow", spawnPos, Quaternion.Euler(90, 0, 0));
                if (arrowObj != null)
                {
                    Arrow arrowScript = arrowObj.GetComponent<Arrow>();
                    if (arrowScript != null) arrowScript.Initialize(randomEnemy.transform);
                }
            }
        }
    }

    public void TriggerArrowVolley()
    {
        if (Time.time < lastArrowTime + arrowVolleyCD) return;
        lastArrowTime = Time.time;

        Debug.Log("Commander Command: ARROW VOLLEY!");
        
        if (commanderAnimator != null) commanderAnimator.SetTrigger(castArrowHash);

        EnemyAI[] enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude);
        if (enemies.Length > 0)
        {
            for (int i = 0; i < 20; i++)
            {
                EnemyAI randomEnemy = enemies[Random.Range(0, enemies.Length)];
                if (randomEnemy != null && !randomEnemy.IsDead)
                {
                    Vector3 spawnPos = randomEnemy.transform.position + new Vector3(Random.Range(-3f, 3f), Random.Range(10f, 15f), Random.Range(-3f, 3f));
                    GameObject arrowObj = ObjectPooler.Instance.SpawnFromPool("Arrow", spawnPos, Quaternion.Euler(90, 0, 0));
                    if (arrowObj != null)
                    {
                        Arrow arrowScript = arrowObj.GetComponent<Arrow>();
                        if (arrowScript != null) arrowScript.Initialize(randomEnemy.transform);
                    }
                }
            }
        }
    }

    public void TriggerFireball(Vector3 ignored)
    {
        if (Time.time < lastMeteorTime + meteorRainCD) return;
        lastMeteorTime = Time.time;

        Debug.Log("Commander Command: METEOR RAIN!");
        
        SyncMagesTrigger(fireballHash);
        if (commanderAnimator != null) commanderAnimator.SetTrigger(fireballHash);

        Vector3 targetPos = FindCrowdedEnemyArea();
        
        // MeteorRain aseti/scripti olan bir objeyi çıkarıyoruz
        GameObject rainObj = ObjectPooler.Instance.SpawnFromPool("MeteorRain", targetPos, Quaternion.identity);
        if (rainObj != null)
        {
            MeteorRain rainScript = rainObj.GetComponent<MeteorRain>();
            if (rainScript != null) rainScript.Initialize(targetPos);
        }
    }

    private Vector3 FindCrowdedEnemyArea()
    {
        EnemyAI[] enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude);
        if (enemies.Length == 0) return Vector3.zero;

        // Basitçe en çok düşmanın olduğu merkezi bul (veya en öndeki kalabalığı)
        Vector3 averagePos = Vector3.zero;
        int count = 0;
        foreach (var e in enemies)
        {
            if (!e.IsDead)
            {
                averagePos += e.transform.position;
                count++;
            }
        }
        return count > 0 ? averagePos / count : Vector3.zero;
    }

    public void TriggerLightningStrike(Vector3 targetPosition)
    {
        if (Time.time < lastLightningTime + lightningCD) return;
        lastLightningTime = Time.time;

        Debug.Log("Commander Command: LIGHTNING STRIKE!");
        if (commanderAnimator != null) commanderAnimator.SetTrigger(castLightningHash);
        
        ObjectPooler.Instance.SpawnFromPool("Lightning", targetPosition, Quaternion.identity);

        Collider[] hitColliders = Physics.OverlapSphere(targetPosition, lightningRadius);
        foreach (var hitCollider in hitColliders)
        {
            EnemyAI enemy = hitCollider.GetComponent<EnemyAI>();
            if (enemy != null) enemy.TakeDamage(lightningDamage);
        }
    }

    public void SetFortifyState(bool active)
    {
        IsFortifying = active;
        if (commanderAnimator != null) commanderAnimator.SetBool(isFortifyingHash, active);
        
        // Büyücüleri de senkronize et (Duruş/Loop için)
        SyncMagesBool(isFortifyingHash, active);

        if (!active) 
        {
            fortifyTimer = 0f;
        }
    }

    private void Update()
    {
        if (IsFortifying)
        {
            fortifyTimer += Time.deltaTime;
            if (fortifyTimer >= fortifyHoldRequired)
            {
                ApplyFortifyHeal();
                SetFortifyState(false); // Başarıyla bitti, boz
            }
        }
    }

    private void ApplyFortifyHeal()
    {
        if (Time.time < lastFortifyTime + fortifyCD) return;
        lastFortifyTime = Time.time;

        Debug.Log("Commander Command: FORTIFY SUCCESS! HEALING STRUCTURES...");
        
        StructureManager[] structures = Object.FindObjectsByType<StructureManager>(FindObjectsInactive.Exclude);
        foreach (var structure in structures)
        {
            if (!structure.IsDestroyed) // Yıkılmış binalar hariç
            {
                structure.Heal(healAmount);
                // Görsel geri bildirim: Yeşil artılar vs.
                ObjectPooler.Instance.SpawnFromPool("HealEffect", structure.transform.position + Vector3.up * 2f, Quaternion.identity);
            }
        }
    }

    private void SyncMagesBool(int hash, bool value)
    {
        MageAI[] mages = Object.FindObjectsByType<MageAI>(FindObjectsInactive.Exclude);
        foreach (var mage in mages)
        {
            Animator mAnim = mage.GetComponent<Animator>();
            if (mAnim != null) mAnim.SetBool(hash, value);
        }
    }

    private void SyncMagesTrigger(int hash)
    {
        MageAI[] mages = Object.FindObjectsByType<MageAI>(FindObjectsInactive.Exclude);
        foreach (var mage in mages)
        {
            Animator mAnim = mage.GetComponent<Animator>();
            if (mAnim != null) mAnim.SetTrigger(hash);
        }
    }
}

