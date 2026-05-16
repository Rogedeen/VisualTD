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
        // Billboard effect: Make the health bar always face the camera (canvas style)
        if (mainCamera != null)
        {
            // Only rotate to face camera, but maintain local Z orientation
            Vector3 dirToCamera = mainCamera.position - transform.position;
            if (dirToCamera != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(-dirToCamera, Vector3.up);
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
