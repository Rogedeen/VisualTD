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
            string gName = data.gesture.ToLower().Replace("_", " ").Trim();
            if (isPaused && gName != "pause" && gName != "palm") continue;

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

        switch (gestureName.ToLower().Replace("_", " ").Trim())
        {
            case "hold fire":
            case "hold_fire":
                if (SkillManager.Instance != null) SkillManager.Instance.HoldArchers(true);
                break;
            case "arrow volley":
            case "arrow_volley":
                if (SkillManager.Instance != null) SkillManager.Instance.HoldArchers(false); 
                if (SkillManager.Instance != null) SkillManager.Instance.TriggerArrowVolley();
                break;
            case "lightning":
            case "lightning strike":
            case "lightning_strike":
            case "spiderman cast":
            case "spiderman_cast":
                if (SkillManager.Instance != null) SkillManager.Instance.SetFortifyState(false);
                if (SkillManager.Instance != null) SkillManager.Instance.TriggerLightningStrike(Vector3.zero);
                break;
            case "fortify":
            case "fortify wall":
            case "fortify_wall":
                if (SkillManager.Instance != null) SkillManager.Instance.SetFortifyState(true);
                break;
            case "fireball":
            case "fireball cast":
            case "fireball_cast":
                if (SkillManager.Instance != null) SkillManager.Instance.SetFortifyState(false);
                if (SkillManager.Instance != null) SkillManager.Instance.TriggerFireball(Vector3.zero);
                break;
            case "pause":
            case "palm":
                if (GameManager.Instance != null) GameManager.Instance.TogglePause();
                break;
            case "upgrade 1":
            case "upgrade_1":
                if (GameManager.Instance != null) GameManager.Instance.PurchaseUpgrade(1);
                break;
            case "upgrade 2":
            case "upgrade_2":
                if (GameManager.Instance != null) GameManager.Instance.PurchaseUpgrade(2);
                break;
            case "upgrade 3":
            case "upgrade_3":
                if (GameManager.Instance != null) GameManager.Instance.PurchaseUpgrade(3);
                break;
            case "upgrade 4":
            case "upgrade_4":
                if (GameManager.Instance != null) GameManager.Instance.PurchaseUpgrade(4);
                break;
            default:
                Debug.LogWarning($"[GestureParser] Unknown gesture received: {gestureName}");
                break;
        }
    }
}
