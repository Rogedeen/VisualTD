using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Tooltip("The Image component that represents the health fill. Must be Image Type: Filled")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Gradient healthGradient; // Optional: colors from low to high health
    
    private Transform mainCamera;

    private void Start()
    {
        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        // Billboard effect: Only rotate the UI image to face camera, NOT the parent transform
        if (mainCamera != null && fillImage != null)
        {
            // Only the fill image rotates; the parent (enemy) rotation stays unchanged
            Vector3 dirToCamera = mainCamera.position - fillImage.transform.position;
            if (dirToCamera != Vector3.zero)
            {
                fillImage.transform.rotation = Quaternion.LookRotation(-dirToCamera, Vector3.up);
            }
        }
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (fillImage == null) return;

        float fillAmount = currentHealth / maxHealth;
        fillImage.fillAmount = fillAmount;
        fillImage.color = healthGradient.Evaluate(fillAmount);
    }
}
