using UnityEngine;

public class MageAI : MonoBehaviour
{
    [Header("Mage Settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject fireballPrefab;

    private Animator animator;
    private Transform currentTarget;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        
        RandomizeAnimation();
    }

    private void OnEnable()
    {
        RandomizeAnimation();
    }

    private void RandomizeAnimation()
    {
        if (animator != null)
        {
            // Farklı karelerde başlamalarını sağlar
            animator.Play(0, -1, Random.value);
            // Hızlarını hafif değiştirir
            animator.speed = Random.Range(0.85f, 1.15f);
        }
    }

    private void Update()
    {
        // Mage'ler sadece SkillManager komutlarıyla çalışacak, kendi kendilerine hareket etmeyecekler.
    }
}
