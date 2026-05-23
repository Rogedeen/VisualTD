using UnityEngine;

public class ReturnToPoolAfterTime : MonoBehaviour
{
    [SerializeField] private float delay = 2f;
    [SerializeField] private string poolTag;
    [SerializeField] private bool disableAfterTime = false;

    private void OnEnable()
    {
        CancelInvoke();
        if (disableAfterTime)
            Invoke(nameof(SimpleDisable), delay);
        else
            Invoke(nameof(Return), delay);
    }

    private void SimpleDisable()
    {
        gameObject.SetActive(false);
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
