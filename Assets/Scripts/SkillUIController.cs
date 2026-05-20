using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class SkillUIController : MonoBehaviour
{
    private VisualElement root;
    private SkillSlotUI arrowSlot, lightningSlot, fortifySlot, meteorSlot;
    private UpgradeSlotUI wallUpgrade, towerUpgrade, mageUpgrade;

    private void OnEnable()
    {
        UIDocument uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) return;
        
        root = uiDoc.rootVisualElement;

        // Skill slotlarını bağla
        arrowSlot = new SkillSlotUI(root.Q("Skill_Arrow"));
        lightningSlot = new SkillSlotUI(root.Q("Skill_Lightning"));
        fortifySlot = new SkillSlotUI(root.Q("Skill_Fortify"));
        meteorSlot = new SkillSlotUI(root.Q("Skill_Meteor"));

        // Upgrade slotlarını bağla
        wallUpgrade = new UpgradeSlotUI(root.Q("Upgrade_Wall"));
        towerUpgrade = new UpgradeSlotUI(root.Q("Upgrade_Tower"));
        mageUpgrade = new UpgradeSlotUI(root.Q("Upgrade_Mage"));
    }

    private void Update()
    {
        if (SkillManager.Instance == null) return;

        arrowSlot.UpdateProgress(SkillManager.Instance.GetArrowProgress(), this);
        lightningSlot.UpdateProgress(SkillManager.Instance.GetLightningProgress(), this);
        fortifySlot.UpdateProgress(SkillManager.Instance.GetFortifyProgress(), this);
        meteorSlot.UpdateProgress(SkillManager.Instance.GetMeteorProgress(), this);

        if (UpgradeManager.Instance != null && GameManager.Instance != null)
        {
            wallUpgrade.UpdateState(GameManager.Instance.GetGold(), UpgradeManager.Instance.wallUpgradeCost, this);
            towerUpgrade.UpdateState(GameManager.Instance.GetGold(), UpgradeManager.Instance.towerUpgradeCost, this);
            mageUpgrade.UpdateState(GameManager.Instance.GetGold(), UpgradeManager.Instance.mageUpgradeCost, this);
        }
    }

    private class UpgradeSlotUI
    {
        private VisualElement icon;
        private Label costLabel;
        private bool canAfford = false;

        public UpgradeSlotUI(VisualElement slot)
        {
            if (slot == null) return;
            icon = slot.Q<VisualElement>(className: "upgrade-icon");
            costLabel = slot.Q<Label>(className: "upgrade-cost");
        }

        public void UpdateState(int currentGold, int cost, MonoBehaviour owner)
        {
            if (icon == null || costLabel == null) return;

            costLabel.text = cost + "G";

            if (currentGold >= cost)
            {
                if (!canAfford)
                {
                    canAfford = true;
                    owner.StartCoroutine(PlayReadyEffect());
                }
                icon.style.borderTopColor = Color.green;
                icon.style.borderBottomColor = Color.green;
                icon.style.borderLeftColor = Color.green;
                icon.style.borderRightColor = Color.green;
            }
            else
            {
                canAfford = false;
                icon.style.borderTopColor = Color.red;
                icon.style.borderBottomColor = Color.red;
                icon.style.borderLeftColor = Color.red;
                icon.style.borderRightColor = Color.red;
            }
        }

        private IEnumerator PlayReadyEffect()
        {
            icon.AddToClassList("ready-to-buy");
            yield return new WaitForSeconds(0.5f);
            icon.RemoveFromClassList("ready-to-buy");
        }
    }

    private class SkillSlotUI
    {
        private VisualElement icon;
        private VisualElement overlay;
        private bool isReady = false;

        public SkillSlotUI(VisualElement slot)
        {
            if (slot == null) return;
            icon = slot.Q<VisualElement>(className: "skill-icon");
            overlay = slot.Q<VisualElement>(className: "skill-cooldown-overlay");
        }

        public void UpdateProgress(float progress, MonoBehaviour owner)
        {
            if (icon == null || overlay == null) return;

            // Cooldown doluluk oranı (Yükseklik azalır)
            overlay.style.height = Length.Percent((1f - progress) * 100f);

            // Renk Geçişi: Kırmızı -> Turuncu -> Sarı -> Yeşil
            Color targetColor;
            if (progress < 0.5f)
                targetColor = Color.Lerp(Color.red, new Color(1f, 0.5f, 0f), progress * 2f);
            else
                targetColor = Color.Lerp(new Color(1f, 0.5f, 0f), Color.green, (progress - 0.5f) * 2f);

            icon.style.borderTopColor = targetColor;
            icon.style.borderBottomColor = targetColor;
            icon.style.borderLeftColor = targetColor;
            icon.style.borderRightColor = targetColor;

            // Hazır olma efekti (1 defalık parlayıp büyüme)
            if (progress >= 1f && !isReady)
            {
                isReady = true;
                owner.StartCoroutine(PlayReadyEffect());
            }
            else if (progress < 1f)
            {
                isReady = false;
            }
        }

        private IEnumerator PlayReadyEffect()
        {
            icon.AddToClassList("skill-ready");
            // Hafif bir parlama ve büyüme CSS tarafında transition ile yapılıyor
            yield return new WaitForSeconds(0.5f);
            icon.RemoveFromClassList("skill-ready");
            
            // Sabit yeşil ve hazır durumda kalması için class'ı tekrar değil farklı bir state ekleyelim?
            // User: "en sonda da yeşil olacak" demiş, zaten borderColor yeşil kalıyor.
        }
    }
}
