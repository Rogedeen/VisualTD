using UnityEngine;

public class BackgroundRunner : MonoBehaviour
{
    private void Awake()
    {
        // Unity'nin arka planda (başka pencereye odaklıyken) çalışmaya devam etmesini sağlar
        Application.runInBackground = true;
        
        // FPS düşüşünü engellemek için hedef FPS'i sabitleyelim
        Application.targetFrameRate = 60;
        
        Debug.Log("Background Running Enabled: Game will continue to run when out of focus.");
    }
}
