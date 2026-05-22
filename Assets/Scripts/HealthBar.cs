using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The Slider component for health display")]
    [SerializeField] private Slider healthSlider;
    
    [Header("Colors (Optional)")]
    [SerializeField] private Gradient healthGradient; // Can azaldıkça rengin değişmesi için
    [SerializeField] private Image fillImage;        // Gradient uygulayacaksak Fill Image gerekir

    [Header("Billboard Settings")]
    [Tooltip("If this script is on the root object, drag the UI/Canvas child here. If null, rotates this object.")]
    [SerializeField] private Transform billboardTransform;

    private Transform mainCamera;

    private void Start()
    {
        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }

        // AUTO-DETECTION: Don't rotate the parent! 
        // Find the specific child that should be face the camera (Canvas or UI container)
        if (billboardTransform == null)
        {
            // 1. Try to find a child Canvas first (standard for world-space health bars)
            Canvas childCanvas = GetComponentInChildren<Canvas>();
            if (childCanvas != null && childCanvas.transform != transform)
            {
                billboardTransform = childCanvas.transform;
            }
            // 2. Fallback to any child that looks like a UI element
            else
            {
                foreach (Transform child in transform)
                {
                    if (child.name.ToLower().Contains("canvas") || 
                        child.name.ToLower().Contains("health") || 
                        child.name.ToLower().Contains("ui") ||
                        child.name.ToLower().Contains("bar"))
                    {
                        billboardTransform = child;
                        break;
                    }
                }
            }
        }

        if (billboardTransform == transform) billboardTransform = null;
    }

    private void LateUpdate()
    {
        // Billboard effect: Only rotate the target (Health Bar visual) to face camera
        // CRITICAL: billboardTransform MUST be a child, otherwise the whole object (Tower/Enemy) rotates!
        if (mainCamera != null && billboardTransform != null && billboardTransform != transform)
        {
            Vector3 direction = mainCamera.position - billboardTransform.position;
            direction.y = 0; // Keep it upright
            
            if (direction != Vector3.zero)
            {
                billboardTransform.rotation = Quaternion.LookRotation(-direction);
            }
        }
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (healthSlider == null) return;

        float fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        healthSlider.value = fillAmount;
        
        // Eğer gradient (renk geçişi) kullanıyorsan:
        if (healthGradient != null && fillImage != null)
        {
            fillImage.color = healthGradient.Evaluate(fillAmount);
        }
    }
}
