using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Tooltip("The Image component that represents the health fill. Must be Image Type: Filled")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Gradient healthGradient; // Optional: colors from low to high health
    
    [Header("Colors")]
    [SerializeField] private Color fullColor = Color.red;
    [SerializeField] private Color emptyColor = Color.white;

    private Transform mainCamera;

    private void Start()
    {
        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }

        // Başlangıçta renkleri ayarla
        if (fillImage != null)
        {
            // Eğer Image Type: Filled ise arkadaki beyazlık Background objesinden gelir.
            // Biz sadece doluluk rengini kırmızı yapıyoruz.
            fillImage.color = fullColor;
            
            // Eğer arka planı kodla beyaz yapmak istersen:
            Transform bg = transform.Find("Background");
            if (bg != null)
            {
                Image bgImg = bg.GetComponent<Image>();
                if (bgImg != null) bgImg.color = emptyColor;
            }
        }
    }

    private void LateUpdate()
    {
        // Billboard effect: Only rotate the UI image to face camera
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.forward);
        }
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (fillImage == null) return;

        float fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        
        // --- SMOTTH SLIDER (OPSİYONEL): Direkt setlemek yerine Coroutine ile de yapılabilir 
        // ama performans için şimdilik direkt setliyoruz ---
        fillImage.fillAmount = fillAmount;
        
        // Eğer gradient kullanılmıyorsa direkt renk geçişi (Opsiyonel)
        if (healthGradient == null || healthGradient.colorKeys.Length <= 1)
        {
            fillImage.color = Color.Lerp(emptyColor, fullColor, fillAmount);
        }
        else
        {
            fillImage.color = healthGradient.Evaluate(fillAmount);
        }
    }
}
