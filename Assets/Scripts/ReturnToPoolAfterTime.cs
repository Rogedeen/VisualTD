using UnityEngine;

public class ReturnToPoolAfterTime : MonoBehaviour
{
    [SerializeField] private float delay = 2f;
    [SerializeField] private string poolTag;

    private void OnEnable()
    {
        CancelInvoke();
        Invoke(nameof(Return), delay);
    }

    private void Return()
    {
        if (ObjectPooler.Instance != null && !string.IsNullOrEmpty(poolTag))
        {
            ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
