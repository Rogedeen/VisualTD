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
        
        switch (gestureName)
        {
            case "Arrow_Volley":
                // TODO: Link to SkillManager.Instance.CastArrowVolley()
                Debug.Log(">>> ACTION: Arrow Volley Casted!");
                break;
            case "Lightning_Strike":
                // TODO: Link to SkillManager.Instance.CastLightningStrike()
                Debug.Log(">>> ACTION: Lightning Strike Casted!");
                break;
            case "Fortify_Wall":
                // TODO: Link to SkillManager.Instance.CastFortifyWall()
                Debug.Log(">>> ACTION: Wall Fortified!");
                break;
            default:
                Debug.LogWarning($"[GestureParser] Unknown gesture received: {gestureName}");
                break;
        }
    }
}
