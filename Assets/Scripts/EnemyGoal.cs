using UnityEngine;

public class EnemyGoal : MonoBehaviour
{
    public static EnemyGoal Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Eğer collider'ın veya üst objesinin EnemyAI componenti varsa düşmandır.
        EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy == null)
        {
            enemy = other.GetComponent<EnemyAI>();
        }

        if (enemy != null && !enemy.IsDead)
        {
            if (GameManager.Instance != null)
            {
                // Her bir düşman geçtiğinde oyuncu canından 1 düşer
                GameManager.Instance.TakePlayerDamage(1);
            }
            
            // Düşmanı anında sahneden kaldırıyoruz
            ObjectPooler.Instance.ReturnToPool(enemy.PoolTag, enemy.gameObject);
        }
    }
}
