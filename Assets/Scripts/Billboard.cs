using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCamera;

    private void Start()
    {
        if (Camera.main != null)
            mainCamera = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (mainCamera == null) return;

        // Objeyi kameraya doğru döndürür (Y eksenini kilitleyerek dik durmasını sağlar)
        Vector3 direction = mainCamera.position - transform.position;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(-direction);
        }
    }
}
