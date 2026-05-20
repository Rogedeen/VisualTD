using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("Upgrade Costs")]
    public int wallUpgradeCost = 50;
    public int towerUpgradeCost = 100;
    public int mageUpgradeCost = 150;

    [Header("Upgrade Levels")]
    public int wallLevel = 1;
    public int towerLevel = 1;
    public int mageLevel = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpgradeWall()
    {
        if (GameManager.Instance.SpendGold(wallUpgradeCost))
        {
            wallLevel++;
            Debug.Log($"Wall upgraded to level {wallLevel}");

            StructureManager[] structures = Object.FindObjectsByType<StructureManager>(FindObjectsInactive.Exclude);
            foreach (var s in structures)
            {
                if (s.type == StructureType.Wall || s.type == StructureType.Gate)
                {
                    s.UpgradeMaxHealth(1.2f); // %20 Max HP Artışı
                    s.Heal(500); // Seviye atlayınca tamir et
                }
            }
            wallUpgradeCost = Mathf.RoundToInt(wallUpgradeCost * 1.5f);
        }
    }

    public void UpgradeTower()
    {
        if (GameManager.Instance.SpendGold(towerUpgradeCost))
        {
            towerLevel++;
            Debug.Log($"Tower upgraded to level {towerLevel}");

            ArcherAI[] archers = Object.FindObjectsByType<ArcherAI>(FindObjectsInactive.Exclude);
            foreach (var a in archers)
            {
                a.damageMultiplier += 0.2f; // Her seviye %20 Hasar Artışı
            }
            towerUpgradeCost = Mathf.RoundToInt(towerUpgradeCost * 1.5f);
        }
    }

    public void UpgradeMage()
    {
        if (GameManager.Instance.SpendGold(mageUpgradeCost))
        {
            mageLevel++;
            Debug.Log($"Mage upgraded to level {mageLevel}");

            // SkillManager üzerindeki cooldownları kalıcı olarak düşür
            SkillManager.Instance.UpgradeMageAbilities(0.9f); // %10 daha hızlı dolum

            mageUpgradeCost = Mathf.RoundToInt(mageUpgradeCost * 1.5f);
        }
    }
}
