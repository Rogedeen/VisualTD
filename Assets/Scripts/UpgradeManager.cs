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
            wallUpgradeCost = Mathf.RoundToInt(wallUpgradeCost * 1.5f);
            Debug.Log($"Wall upgraded to level {wallLevel}");
            // Uygulama: Tüm duvarların canını artır veya iyileştir
            StructureManager[] structures = Object.FindObjectsByType<StructureManager>(FindObjectsInactive.Exclude);
            foreach(var s in structures) if(s.type == StructureType.Wall) s.Heal(500);
        }
    }

    public void UpgradeTower()
    {
        if (GameManager.Instance.SpendGold(towerUpgradeCost))
        {
            towerLevel++;
            towerUpgradeCost = Mathf.RoundToInt(towerUpgradeCost * 1.5f);
            Debug.Log($"Tower upgraded to level {towerLevel}");
        }
    }

    public void UpgradeMage()
    {
        if (GameManager.Instance.SpendGold(mageUpgradeCost))
        {
            mageLevel++;
            mageUpgradeCost = Mathf.RoundToInt(mageUpgradeCost * 1.5f);
            Debug.Log($"Mage upgraded to level {mageLevel}");
        }
    }
}
