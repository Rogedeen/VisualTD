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
        private VisualElement frame;
        private Label costLabel;
        private bool canAfford = false;

        public UpgradeSlotUI(VisualElement slot)
        {
            if (slot == null) return;
            frame = slot.Q<VisualElement>(className: "upgrade-frame");
            costLabel = slot.Q<Label>(className: "card-cost");
        }

        public void UpdateState(int currentGold, int cost, MonoBehaviour owner)
        {
            if (frame == null || costLabel == null) return;

            costLabel.text = cost + "G";

            if (currentGold >= cost)
            {
                if (!canAfford)
                {
                    canAfford = true;
                    owner.StartCoroutine(PlayReadyEffect());
                }
                frame.style.borderTopColor = new Color(1f, 0.84f, 0f); // Gold
                frame.style.borderBottomColor = new Color(1f, 0.84f, 0f);
                frame.style.borderLeftColor = new Color(1f, 0.84f, 0f);
                frame.style.borderRightColor = new Color(1f, 0.84f, 0f);
            }
            else
            {
                canAfford = false;
                frame.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f); // Gray
                frame.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                frame.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
                frame.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            }
        }

        private IEnumerator PlayReadyEffect()
        {
            frame.AddToClassList("ready-to-buy");
            yield return new WaitForSeconds(0.5f);
            frame.RemoveFromClassList("ready-to-buy");
        }
    }

    private class SkillSlotUI
    {
        private VisualElement orbInner;
        private VisualElement overlay;
        private VisualElement slotRoot;
        private bool isReady = false;

        public SkillSlotUI(VisualElement slot)
        {
            if (slot == null) return;
            slotRoot = slot;
            orbInner = slot.Q<VisualElement>(className: "orb-inner");
            overlay = slot.Q<VisualElement>(className: "orb-wipe");
        }

        public void UpdateProgress(float progress, MonoBehaviour owner)
        {
            if (slotRoot == null || overlay == null) return;

            overlay.style.height = Length.Percent((1f - progress) * 100f);

            Color targetColor;
            if (progress < 1f)
            {
                targetColor = Color.Lerp(new Color(0.5f, 0f, 0f), new Color(1f, 0.5f, 0f), progress);
            }
            else
            {
                targetColor = new Color(1f, 0.84f, 0f); // Gold when ready
            }

            slotRoot.style.borderTopColor = targetColor;
            slotRoot.style.borderBottomColor = targetColor;
            slotRoot.style.borderLeftColor = targetColor;
            slotRoot.style.borderRightColor = targetColor;

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
            slotRoot.AddToClassList("skill-ready");
            yield return new WaitForSeconds(0.5f);
            slotRoot.RemoveFromClassList("skill-ready");
        }
    }
}
