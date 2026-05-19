using UnityEngine;
using UnityEngine.Events;

public enum StructureType { Gate, Wall, Tower }

public class StructureManager : MonoBehaviour
{
    public StructureType type = StructureType.Wall;
    [SerializeField] private float maxHealth = 300f;
    private float currentHealth;

    [Header("Destruction Effects")]
    [SerializeField] private ParticleSystem dustParticlePrefab;

    [Header("UI & Feedback")]
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private DamageFlash damageFlash;

    public UnityEvent OnStructureDestroyed;
    private bool isDestroyed = false;
    public bool IsDestroyed => isDestroyed;

    private UnityEngine.AI.NavMeshObstacle obstacle;

    private void Start()
    {
        currentHealth = maxHealth;
        obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();

        if (healthBar == null) healthBar = GetComponentInChildren<HealthBar>();
        if (damageFlash == null) damageFlash = GetComponent<DamageFlash>();
        
        if (healthBar != null) healthBar.UpdateHealth(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (isDestroyed) return;

        currentHealth -= amount;
        
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
                    // Ayrıl - okçu artık bağımsız olsun
                    archer.transform.SetParent(null);
                    archer.FallAndDie();
                }

                // Kule görsellerini ve collider'ını kapat (çocuk objeleri arka planda kalabilir)
                Renderer[] rends = GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends) r.enabled = false;

                // Baz collider zaten kapatıldı; engeli devre dışı bırakıldı.

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
