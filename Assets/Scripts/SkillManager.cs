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
    [SerializeField] private ChainLightningSkill chainLightning;

    // Cooldown Progress Helpers (0 to 1)
    public float GetArrowProgress() => Mathf.Clamp01((Time.time - lastArrowTime) / arrowVolleyCD);
    public float GetLightningProgress() => Mathf.Clamp01((Time.time - lastLightningTime) / lightningCD);
    public float GetFortifyProgress() => Mathf.Clamp01((Time.time - lastFortifyTime) / fortifyCD);
    public float GetMeteorProgress() => Mathf.Clamp01((Time.time - lastMeteorTime) / meteorRainCD);

    public void UpgradeMageAbilities(float cdMultiplier)
    {
        lightningCD *= cdMultiplier;
        meteorRainCD *= cdMultiplier;
        lightningDamage *= 1.2f; // Hasarı da artır
        Debug.Log($"Mage Skills Upgraded! New Meteor CD: {meteorRainCD}");
    }

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
    private readonly int isAttackingHash = Animator.StringToHash("isAttacking");
    private readonly int fireballHash = Animator.StringToHash("fireball");
    private readonly int attackHash = Animator.StringToHash("attack");
    private readonly int castLightningHashLegacy = Animator.StringToHash("CastLightning");
    
    // Legacy / Other
    private readonly int castArrowHash = Animator.StringToHash("attack");

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
            // --- DÜZELTME: Okları farklı düşmanlara dağıtıyoruz ---
            int arrowCount = 20;
            for (int i = 0; i < arrowCount; i++)
            {
                // Her ok için listeden rastgele bir düşman seçiyoruz
                EnemyAI randomEnemy = enemies[Random.Range(0, enemies.Length)];
                
                if (randomEnemy != null && !randomEnemy.IsDead)
                {
                    // Her düşmanın tepesinde biraz daha geniş bir alandan oklar yağsın
                    Vector3 spawnPos = randomEnemy.transform.position + new Vector3(Random.Range(-3f, 3f), Random.Range(15f, 20f), Random.Range(-3f, 3f));
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

    public void TriggerFireball(Vector3 targetPosFromPython)
    {
        if (Time.time < lastMeteorTime + meteorRainCD) 
        {
            Debug.Log("Meteor on Cooldown!");
            return;
        }
        lastMeteorTime = Time.time;

        Debug.Log("Commander Command: METEOR RAIN!");
        
        // Animasyonu başlat
        SyncMagesBool(isAttackingHash, true);
        if (commanderAnimator != null) commanderAnimator.SetBool(isAttackingHash, true);

        // --- DÜZELTME: Python'dan gelen veya kalabalık olan yeri hedef al ---
        Vector3 targetPos = targetPosFromPython != Vector3.zero ? targetPosFromPython : FindCrowdedEnemyArea();
        
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
        
        // --- DÜZELTME: Hedef pozisyon sıfırsa en kalabalık alanı bul ---
        Vector3 finalTargetPos = targetPosition != Vector3.zero ? targetPosition : FindCrowdedEnemyArea();
        
        // Eğer hala sıfırsa (hiç düşman yoksa) çık
        if (finalTargetPos == Vector3.zero) return;

        lastLightningTime = Time.time;

        Debug.Log("Commander Command: CHAIN LIGHTNING!");
        
        // Animator parametresini kontrol et ve tetikle
        if (commanderAnimator != null)
        {
            // Eğer "CastLightning" (yeni) yoksa, büyücülerin kullandığı "attack" veya "isAttacking" parametrelerini de deneyebiliriz.
            // Ama en sağlıklısı Animator'da CastLightning olana kadar fallback olarak attack kullanmak.
            bool triggerSet = false;
            foreach (var param in commanderAnimator.parameters)
            {
                if (param.nameHash == castLightningHash)
                {
                    commanderAnimator.SetTrigger(castLightningHash);
                    triggerSet = true;
                    break;
                }
            }
            
            if (!triggerSet)
            {
                Debug.LogWarning("[SkillManager] Animator'da 'CastLightning' bulunamadı, 'attack' tetikleniyor.");
                commanderAnimator.SetTrigger(attackHash); // Mevcut olan 'attack' animasyonuna fallback yapar
            }
        }
        
        if (chainLightning != null)
        {
            chainLightning.Execute(finalTargetPos);
        }
        else
        {
            Debug.LogWarning("[SkillManager] ChainLightningSkill referansı atanmamış! Temel yıldırım çalışıyor.");
            // Fallback to basic strike if chain is not assigned
            ObjectPooler.Instance.SpawnFromPool("Lightning", finalTargetPos, Quaternion.identity);

            Collider[] hitColliders = Physics.OverlapSphere(finalTargetPos, lightningRadius);
            foreach (var hitCollider in hitColliders)
            {
                EnemyAI enemy = hitCollider.GetComponent<EnemyAI>();
                if (enemy != null) enemy.TakeDamage(lightningDamage);
            }
        }
    }

    public void SetFortifyState(bool active)
    {
        if (IsFortifying == active) return; // Gereksiz loop ve tetiklemeyi önle
        
        // Cooldown kontrolü (Eğer aktif edilmeye çalışılıyorsa)
        if (active && Time.time < lastFortifyTime + fortifyCD)
        {
            Debug.Log("Fortify is on cooldown!");
            return;
        }

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
                SetFortifyState(false); // Başarıyla bitti, boz (bu SetFortifyState cooldown time'ı güncellemeyecek)
                lastFortifyTime = Time.time; // Cooldown'ı şimdi başlat
            }
        }
    }

    public void EndMeteorAnimation()
    {
        SyncMagesBool(isAttackingHash, false);
        if (commanderAnimator != null) commanderAnimator.SetBool(isAttackingHash, false);
    }

    private void ApplyFortifyHeal()
    {
        Debug.Log("Commander Command: FORTIFY SUCCESS! HEALING STRUCTURES...");
        
        StructureManager[] structures = Object.FindObjectsByType<StructureManager>(FindObjectsInactive.Exclude);
        
        foreach (var structure in structures)
        {
            if (!structure.IsDestroyed) 
            {
                structure.Heal(healAmount);
                
                // Efekti binanın tam altına (Zero Y) koyuyoruz ki havada durmasın.
                Vector3 spawnPos = structure.transform.position;
                spawnPos.y = 0.1f; 
                
                GameObject healEffect = ObjectPooler.Instance.SpawnFromPool("Heal", spawnPos, Quaternion.identity);
                
                // GÜNCELLEME: Heal animasyonunun 1 kere oynayıp havuzuna dönmesini garanti ediyoruz.
                if (healEffect != null)
                {
                    ReturnToPoolAfterTime returnScript = healEffect.GetComponent<ReturnToPoolAfterTime>();
                    if (returnScript == null) returnScript = healEffect.AddComponent<ReturnToPoolAfterTime>();
                    
                    // Inspector ayarlarını kodla ezerek efektin kalıcı olmasını engelliyoruz
                    // 2 saniye animasyon için makul bir süre
                }
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

