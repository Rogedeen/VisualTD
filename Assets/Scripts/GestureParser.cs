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
            // OYUN DURAKLATILDIYSA (ve gelen hareket PAUSE değilse) hareketleri işleme!
            bool isPaused = Time.timeScale == 0f;
            if (isPaused && data.gesture != "Palm") continue;

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
                if (SkillManager.Instance != null) SkillManager.Instance.HoldArchers(true);
                break;
            case "Arrow_Volley":
                if (SkillManager.Instance != null) SkillManager.Instance.HoldArchers(false); 
                if (SkillManager.Instance != null) SkillManager.Instance.TriggerArrowVolley();
                break;
            case "Lightning_Strike":
                if (SkillManager.Instance != null) SkillManager.Instance.SetFortifyState(false);
                if (SkillManager.Instance != null) SkillManager.Instance.TriggerLightningStrike(Vector3.zero);
                break;
            case "Fortify_Wall":
                if (SkillManager.Instance != null) SkillManager.Instance.SetFortifyState(true);
                break;
            case "Spiderman_Cast":
                if (SkillManager.Instance != null) SkillManager.Instance.SetFortifyState(false);
                if (SkillManager.Instance != null) SkillManager.Instance.TriggerLightningStrike(Vector3.zero);
                break;
            case "Fireball_Cast":
                if (SkillManager.Instance != null) SkillManager.Instance.SetFortifyState(false);
                if (SkillManager.Instance != null) SkillManager.Instance.TriggerFireball(Vector3.zero);
                break;
            case "Palm": // "Time_Out" yerine daha doğal "Palm" hareketi
                if (GameManager.Instance != null) GameManager.Instance.TogglePause();
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
