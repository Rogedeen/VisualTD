using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Positions")]
    [SerializeField] private Vector3 gameplayPos = new Vector3(1.15f, 4.51f, -5.21f);
    [SerializeField] private Vector3 gameplayRot = new Vector3(38.92f, 1.54f, 0f);
    
    [SerializeField] private Vector3 menuPos = new Vector3(15f, 6f, -15f); // Side-angle view of the castle
    [SerializeField] private Vector3 menuRot = new Vector3(15f, -30f, 0f); // Looking towards the gate area

    [Header("Settings")]
    [SerializeField] private float transitionDuration = 2.0f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine transitionCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Start in menu position
        transform.position = menuPos;
        transform.rotation = Quaternion.Euler(menuRot);
    }

    public void SwitchToGameplay()
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionTo(gameplayPos, Quaternion.Euler(gameplayRot)));
    }

    public void SwitchToMenu()
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionTo(menuPos, Quaternion.Euler(menuRot)));
    }

    private IEnumerator TransitionTo(Vector3 targetPos, Quaternion targetRot)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled because game might be paused
            float t = transitionCurve.Evaluate(elapsed / transitionDuration);
            
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;
    }
}
