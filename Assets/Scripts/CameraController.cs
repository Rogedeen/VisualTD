using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Positions")]
    [SerializeField] private Vector3 gameplayPos;
    [SerializeField] private Vector3 gameplayRot;
    
    [SerializeField] private Vector3 menuPos; 
    [SerializeField] private Vector3 menuRot;

    [Header("Settings")]
    [SerializeField] private float transitionDuration = 1.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine transitionCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Set initial position immediately without transition
        transform.position = menuPos;
        transform.rotation = Quaternion.Euler(menuRot);
    }

    [ContextMenu("Capture Current as Menu")]
    public void CaptureMenu()
    {
        menuPos = transform.position;
        menuRot = transform.eulerAngles;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Capture Current as Gameplay")]
    public void CaptureGameplay()
    {
        gameplayPos = transform.position;
        gameplayRot = transform.eulerAngles;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
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
