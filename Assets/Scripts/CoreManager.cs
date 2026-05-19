using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class CoreManager : MonoBehaviour
{
    public static CoreManager Instance;

    [SerializeField] private int maxPlayerHealth = 20;
    private int currentPlayerHealth;

    public UnityEvent OnGameOver;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentPlayerHealth = maxPlayerHealth;
        Debug.Log("Core HP: " + currentPlayerHealth);
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyAI enemy = other.GetComponent<EnemyAI>();
        if (enemy != null && !enemy.IsDead)
        {
            // Düşman içeri girdi, oyuncu can kaybeder
            TakeDamage();
            
            // Düşmanı anında öldür/yok et
            enemy.TakeDamage(9999f); 
        }
    }

    private void TakeDamage()
    {
        if (currentPlayerHealth <= 0) return;

        currentPlayerHealth--;
        Debug.Log("Core Hasar Aldı! Kalan Can: " + currentPlayerHealth);

        if (currentPlayerHealth <= 0)
        {
            Debug.Log("GAME OVER!");
            OnGameOver?.Invoke();
            Time.timeScale = 0f; // Oyunu durdur
        }
    }

    // UI butonları veya oyun yeniden başlatma akışı bu metodu çağırabilir
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
