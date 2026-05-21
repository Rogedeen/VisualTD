using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    private bool isPaused = false;
    public int Gold { get; private set; } = 100; // Başlangıç parası
    public int GateHealth { get; private set; } = 1000; // Kapı Canı
    public int PlayerHealth { get; private set; } = 20; // Oyuncu Canı

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void TakePlayerDamage(int amount)
    {
        PlayerHealth -= amount;
        if (PlayerHealth <= 0)
        {
            PlayerHealth = 0;
            GameOver(false);
        }
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        if (CameraController.Instance != null)
        {
            CameraController.Instance.SwitchToGameplay();
        }
        Debug.Log(">>> GAME STARTED! <<<");
    }

    public void GameOver(bool win)
    {
        // Cinematic Slow-Mo
        Time.timeScale = 0.2f;
        
        if (CameraController.Instance != null)
        {
            CameraController.Instance.SwitchToMenu();
        }

        if (win)
        {
            TriggerVictoryCelebration();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowEndMenu(win);
        }
    }

    private void TriggerVictoryCelebration()
    {
        // Find all defense units and trigger victory animation
        var animators = Object.FindObjectsByType<Animator>(FindObjectsInactive.Exclude);
        foreach (var anim in animators)
        {
            // Usually defense units have ArcherAI, MageAI or are in DefenceArmy
            if (anim.GetComponent<ArcherAI>() != null || anim.GetComponent<MageAI>() != null)
            {
                anim.SetTrigger("Victory");
            }
        }
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        Time.timeScale = 1f;
    }

    public void UpdateGateHealth(int currentHealth)
    {
        GateHealth = currentHealth;
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        Debug.Log(isPaused ? ">>> GAME PAUSED (Time Out) <<<" : ">>> GAME RESUMED <<<");
    }

    public void AddGold(int amount)
    {
         Gold += amount;
         if (UIManager.Instance != null) UIManager.Instance.FlashGold();
         Debug.Log($"[Economy] {amount} Altın Kazanıldı! Toplam: {Gold}");
    }

    public int GetGold() => Gold;

    public bool SpendGold(int amount)
    {
        if (Gold >= amount)
        {
            Gold -= amount;
            Debug.Log($"[Economy] {amount} Altın Harcandı! Kalan: {Gold}");
            return true;
        }
        return false;
    }

    public void WinGame()
    {
        GameOver(true);
    }

    public void PurchaseUpgrade(int upgradeId)
    {
        int cost = upgradeId * 50; // Örn: 1. yükseltme 50 altın, 2. yükseltme 100 altın
        
        if (Gold >= cost)
        {
            Gold -= cost;
            Debug.Log($"[Upgrade] Yükseltme {upgradeId} SATIN ALINDI! Kalan Altın: {Gold}");
            
            ApplyUpgradeEffects(upgradeId);
        }
        else
        {
            Debug.Log($"[Upgrade] Yetersiz Altın! Gereken: {cost}, Mevcut: {Gold}");
        }
    }

    private void ApplyUpgradeEffects(int upgradeId)
    {
        switch (upgradeId)
        {
            case 1:
                Debug.Log("Etki: Okçuların hasarı %25 artırıldı!");
                // İlerde ArcherAI üzerinden hasar artışı yapılabilir
                break;
            case 2:
                Debug.Log("Etki: Kapıların maksimum canı %50 artırıldı!");
                break;
            case 3:
                Debug.Log("Etki: Düşman yürüme hızları %20 yavaşlatıldı (Buz büyüsü aktif)!");
                break;
            case 4:
                Debug.Log("Etki: Dev büyü saldırısı (Ultimate) kilidi açıldı!");
                break;
        }
    }
}
