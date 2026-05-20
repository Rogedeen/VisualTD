using UnityEngine;

public class MageAI : MonoBehaviour
{
    [Header("Mage Settings")]
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float fireballCooldown = 3f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject fireballPrefab;

    [Header("Fortify Settings")]
    [SerializeField] private float fortifyRange = 10f;
    [SerializeField] private float fortifyCooldown = 10f;
    [SerializeField] private float fortifyAmount = 100f;

    private Animator animator;
    private float lastFireballTime;
    private float lastFortifyTime;
    private Transform currentTarget;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Mage'ler sadece SkillManager komutlarıyla çalışacak, kendi kendilerine hareket etmeyecekler.
    }

    private void FindNearestEnemy()
    {
        EnemyAI[] enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude);
        float minDistance = Mathf.Infinity;
        currentTarget = null;

        foreach (var enemy in enemies)
        {
            if (enemy.IsDead) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDistance && dist <= attackRange)
            {
                minDistance = dist;
                currentTarget = enemy.transform;
            }
        }
    }

    private void ShootFireball()
    {
        lastFireballTime = Time.time;
        if (animator != null) animator.SetTrigger("fireball");

        // Fireball spawn işlemi
        if (fireballPrefab != null && firePoint != null)
        {
             // Eğer hedef varsa ona doğru fırlat, yoksa düz karşıya
             Quaternion spawnRotation = currentTarget != null 
                 ? Quaternion.LookRotation((currentTarget.position - firePoint.position).normalized) 
                 : firePoint.rotation;

             // Fireball'u ObjectPooler üzerinden çıkarıyoruz (Fireball etiketi havuzda olmalı)
             GameObject ball = ObjectPooler.Instance.SpawnFromPool("Fireball", firePoint.position, spawnRotation);
             
             // Eğer fireball scripti varsa hedefi setleyebiliriz
        }
    }

    private void TryFortifyNearbyStructure()
    {
        // Yakındaki kapı veya köprüyü bulup canını doldurma
        StructureManager[] structures = Object.FindObjectsByType<StructureManager>(FindObjectsInactive.Exclude);
        foreach (var structObj in structures)
        {
            if (Vector3.Distance(transform.position, structObj.transform.position) <= fortifyRange)
            {
                if (structObj.IsDamaged()) // Sadece hasarlıysa güçlendir
                {
                    lastFortifyTime = Time.time;
                    StartCoroutine(TemporaryFortifyAnim());
                    structObj.Heal(fortifyAmount);
                    Debug.Log(structObj.name + " fortified by Mage!");
                    break;
                }
            }
        }
    }

    private System.Collections.IEnumerator TemporaryFortifyAnim()
    {
        if (animator != null) animator.SetBool("isFortifying", true);
        yield return new WaitForSeconds(1.5f);
        if (animator != null) animator.SetBool("isFortifying", false);
    }
}
