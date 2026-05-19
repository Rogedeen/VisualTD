using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private UIDocument uiDocument;
    private Label goldText;
    private Label healthText;
    private Label gestureText;
    private VisualElement gesturePanel;

    private float gestureDisplayTimer = 0f;
    private const float gestureDisplayDuration = 2f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;

        var root = uiDocument.rootVisualElement;
        goldText = root.Q<Label>("GoldText");
        healthText = root.Q<Label>("HealthText");
        gestureText = root.Q<Label>("GestureText");
        gesturePanel = root.Q<VisualElement>("GesturePanel");

        if (gesturePanel != null)
        {
            gesturePanel.style.opacity = 0f; // Başlangıçta görünmez
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null)
        {
            if (goldText != null) goldText.text = GameManager.Instance.Gold.ToString();
            if (healthText != null) healthText.text = GameManager.Instance.BaseHealth.ToString();
        }

        // Gesture panelinin yavaşça kaybolması (Fade out)
        if (gestureDisplayTimer > 0)
        {
            gestureDisplayTimer -= Time.deltaTime;
            if (gesturePanel != null)
            {
                gesturePanel.style.opacity = Mathf.Clamp01(gestureDisplayTimer / 0.5f); // Son 0.5 saniyede fade out
            }
        }
    }

    public void UpdateGestureText(string gestureName)
    {
        if (gestureText != null)
        {
            gestureText.text = gestureName.Replace("_", " ").ToUpper();
        }

        if (gesturePanel != null)
        {
            gesturePanel.style.opacity = 1f;
        }
        
        gestureDisplayTimer = gestureDisplayDuration;
    }
}
