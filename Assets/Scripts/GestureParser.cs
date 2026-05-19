using UnityEngine;

public class GestureParser : MonoBehaviour
{
    private UDPReceiver udpReceiver;

    void Start()
    {
        udpReceiver = GetComponent<UDPReceiver>();
        if (udpReceiver == null)
        {
            Debug.LogError("[GestureParser] Missing UDPReceiver component! Please attach UDPReceiver to this GameObject.");
        }
    }

    void Update()
    {
        if (udpReceiver == null) return;

        // Process all queued gestures from the background thread safely on the Main Thread
        while (udpReceiver.gestureQueue.TryDequeue(out GestureData data))
        {
            ParseGesture(data.gesture);
        }
    }

    private void ParseGesture(string gestureName)
    {
        Debug.Log($"[GestureParser] Executing action for: {gestureName}");
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateGestureText(gestureName);
        }

        switch (gestureName)
        {
            case "Hold_Fire":
                if (SkillManager.Instance != null) SkillManager.Instance.HoldArchers();
                else Debug.LogError("[GestureParser] SkillManager.Instance is NULL!");
                break;
            case "Arrow_Volley":
                if (SkillManager.Instance != null) SkillManager.Instance.TriggerArrowVolley();
                break;
            case "Lightning_Strike":
                if (SkillManager.Instance != null) SkillManager.Instance.TriggerLightningStrike(Vector3.zero);
                break;
            case "Fortify_Wall":
                if (SkillManager.Instance != null) SkillManager.Instance.TriggerFortifyWall();
                break;
            case "Spiderman_Cast":
                if (SkillManager.Instance != null) SkillManager.Instance.TriggerMageCast();
                break;
            case "Fireball_Cast":
                if (SkillManager.Instance != null) SkillManager.Instance.TriggerFireball();
                break;
            case "Time_Out":
                if (GameManager.Instance != null) GameManager.Instance.TogglePause();
                else Debug.LogError("[GestureParser] GameManager.Instance is NULL!");
                break;
            case "Upgrade_1":
                if (GameManager.Instance != null) GameManager.Instance.PurchaseUpgrade(1);
                break;
            case "Upgrade_2":
                if (GameManager.Instance != null) GameManager.Instance.PurchaseUpgrade(2);
                break;
            case "Upgrade_3":
                if (GameManager.Instance != null) GameManager.Instance.PurchaseUpgrade(3);
                break;
            case "Upgrade_4":
                if (GameManager.Instance != null) GameManager.Instance.PurchaseUpgrade(4);
                break;
            default:
                Debug.LogWarning($"[GestureParser] Unknown gesture received: {gestureName}");
                break;
        }
    }
}
