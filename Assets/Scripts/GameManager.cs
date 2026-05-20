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
            Debug.Log(">>> GAME OVER! KALEN DÜŞTÜ! <<<");
            Time.timeScale = 0f; // Oyunu durdur
        }
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
        Debug.Log(">>> VICTORY! DALGALAR TEMİZLENDİ! <<<");
        // Gelecekte Win UI tetiklemek için burası kullanılacak
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
