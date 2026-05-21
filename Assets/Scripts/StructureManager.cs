using UnityEngine;
using UnityEngine.Events;

public enum StructureType { Gate, Wall, Tower }

public class StructureManager : MonoBehaviour
{
    public StructureType type = StructureType.Wall;
    [SerializeField] private float maxHealth = 300f;
    [SerializeField] private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    [Header("Destruction Effects")]
    [SerializeField] private ParticleSystem dustParticlePrefab;

    [Header("UI & Feedback")]
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private DamageFlash damageFlash;

    public UnityEvent OnStructureDestroyed;
    public bool IsDestroyed => isDestroyed;
    private bool isDestroyed = false;

    private UnityEngine.AI.NavMeshObstacle obstacle;

    private void OnEnable()
    {
        if (TargetManager.Instance != null) TargetManager.Instance.RegisterStructure(this);
    }

    private void OnDisable()
    {
        if (TargetManager.Instance != null) TargetManager.Instance.UnregisterStructure(this);
    }

    private void Start()
    {
        currentHealth = maxHealth;
        // Start'ta tekrar zorla kayıt dene
        if (TargetManager.Instance != null) TargetManager.Instance.RegisterStructure(this);
        else Debug.LogWarning($"[StructureManager] {name} TargetManager'ı bulamadı!");
        
        obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();

        if (healthBar == null) healthBar = GetComponentInChildren<HealthBar>();
        if (damageFlash == null) damageFlash = GetComponent<DamageFlash>();
        
        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);
    }

    public bool IsDamaged()
    {
        return !isDestroyed && currentHealth < maxHealth;
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        
        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);
        if (type == StructureType.Gate && GameManager.Instance != null)
            GameManager.Instance.UpdateGateHealth((int)currentHealth);
    }

    public void UpgradeMaxHealth(float multiplier)
    {
        maxHealth *= multiplier;
        currentHealth *= multiplier; // Mevcut canı da orantılı artır
        
        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);
        if (type == StructureType.Gate && GameManager.Instance != null)
            GameManager.Instance.UpdateGateHealth((int)currentHealth);
    }

    public void TakeDamage(float amount)
    {
        if (isDestroyed) return;

        currentHealth -= amount;
        
        // Eğer bu bir Kapı (Gate) ise GameManager'a haber ver
        if (type == StructureType.Gate && GameManager.Instance != null)
        {
            GameManager.Instance.UpdateGateHealth((int)currentHealth);
        }

        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);
        if (damageFlash != null) damageFlash.Flash();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isDestroyed = true;
            OnStructureDestroyed?.Invoke();
            
            if (dustParticlePrefab != null)
            {
                ParticleSystem dust = Instantiate(dustParticlePrefab, transform.position, Quaternion.identity);
                dust.Play();
                Destroy(dust.gameObject, 3f);
            }

            // Kapı veya Duvar yıkıldığında NavMesh engelini kaldır ki düşmanlar geçebilsin
            if (obstacle != null) obstacle.enabled = false;
            
            // Çarpışmayı kapat
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Kule ise üzerindeki okçuyu serbest bırak ve düşmesini sağla, sonra görseli temizle
            if (type == StructureType.Tower)
            {
                ArcherAI archer = GetComponentInChildren<ArcherAI>();
                if (archer != null)
                {
                    archer.OnTowerDestroyed();
                }

                // Kule görsellerini kapat
                Renderer[] rends = GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends) r.enabled = false;

                // Kısa süre sonra GameObject'i devre dışı bırak (efektler için bekle)
                StartCoroutine(DisableAfterDelay(4f));
            }
            else
            {
                // Görseli hemen kapat (varsayılan duvar/kapı davranışı)
                gameObject.SetActive(false);
            }
        }
    }

    private System.Collections.IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (gameObject != null) gameObject.SetActive(false);
    }

    public void HealWall(float amount)
    {
        if (isDestroyed) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);
    }
}
