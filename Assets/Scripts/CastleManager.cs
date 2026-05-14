using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class CastleManager : MonoBehaviour
{
    [SerializeField] private float maxHealth = 1000f;
    private float currentHealth;

    [Header("Destruction Effects")]
    [Tooltip("The particle system to play when the wall is destroyed (e.g. Dust Cloud)")]
    [SerializeField] private ParticleSystem dustParticlePrefab;
    [Tooltip("How fast the wall shrinks into the ground")]
    [SerializeField] private float collapseDuration = 0.5f;

    public UnityEvent OnCastleDestroyed;
    private bool isDestroyed = false;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (isDestroyed) return;

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isDestroyed = true;
            OnCastleDestroyed?.Invoke();
            Debug.Log("Wall Destroyed!");
            
            // Trigger collapse and dust effect
            StartCoroutine(CollapseWallRoutine());
        }
    }

    public void HealWall(float amount)
    {
        if (isDestroyed) return; // Cannot heal a destroyed wall

        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        Debug.Log($"Wall Healed: {currentHealth}/{maxHealth}");
    }

    private IEnumerator CollapseWallRoutine()
    {
        // 1. Spawn Dust Particle
        if (dustParticlePrefab != null)
        {
            ParticleSystem dust = Instantiate(dustParticlePrefab, transform.position, Quaternion.identity);
            dust.Play();
            Destroy(dust.gameObject, dust.main.duration + 1f); // Cleanup particle object later
        }

        // 2. Collapse Inwards (Scale to zero)
        Vector3 initialScale = transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < collapseDuration)
        {
            elapsedTime += Time.deltaTime;
            float lerpFactor = elapsedTime / collapseDuration;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, lerpFactor);
            yield return null;
        }

        transform.localScale = Vector3.zero;

        // 3. Disable the wall object
        gameObject.SetActive(false);
    }
}
