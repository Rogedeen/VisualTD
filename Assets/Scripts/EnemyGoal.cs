using UnityEngine;

public class EnemyGoal : MonoBehaviour
{
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
            
            // Düşmanı anında sahneden kaldırıyoruz (Object Pool için SetActive(false) yeterlidir)
            enemy.gameObject.SetActive(false);
        }
    }
}
