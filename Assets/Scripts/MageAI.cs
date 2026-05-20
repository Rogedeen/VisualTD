using UnityEngine;

public class MageAI : MonoBehaviour
{
    [Header("Mage Settings")]
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject fireballPrefab;

    [Header("Fortify Settings")]
    [SerializeField] private float fortifyRange = 10f;
    [SerializeField] private float fortifyAmount = 100f;

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
