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
    }

    private void Update()
    {
        // Mage'ler sadece SkillManager komutlarıyla çalışacak, kendi kendilerine hareket etmeyecekler.
    }
}
