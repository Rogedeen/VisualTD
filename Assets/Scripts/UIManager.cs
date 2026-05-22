using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private UIDocument uiDocument;
    private Label goldText;
    private Label healthText;
    private Label playerHealthText;
    private Label gestureText;
    private VisualElement gesturePanel;
    private VisualElement webcamContainer;
    private Image webcamPreview;

    [Header("Menu Elements")]
    private VisualElement startScreen;
    private VisualElement endScreen;
    private Label endTitle;
    private Label endMessage;

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
        
        // HUD
        goldText = root.Q<Label>("GoldText");
        healthText = root.Q<Label>("HealthText");
        playerHealthText = root.Q<Label>("PlayerHealthText");
        gestureText = root.Q<Label>("GestureText");
        gesturePanel = root.Q<VisualElement>("GesturePanel");
        webcamContainer = root.Q<VisualElement>("WebcamContainer");
        webcamPreview = webcamContainer?.Q<Image>("WebcamPreview");

        // Menus
        startScreen = root.Q<VisualElement>("StartScreen");
        endScreen = root.Q<VisualElement>("EndScreen");
        endTitle = root.Q<Label>("EndTitle");
        endMessage = root.Q<Label>("EndMessage");

        // Button Hooks
        root.Q<Button>("BtnStart")?.RegisterCallback<ClickEvent>(ev => StartGame());
        root.Q<Button>("BtnQuit")?.RegisterCallback<ClickEvent>(ev => Application.Quit());
        root.Q<Button>("BtnRestart")?.RegisterCallback<ClickEvent>(ev => GameManager.Instance.RestartGame());
        root.Q<Button>("BtnMenu")?.RegisterCallback<ClickEvent>(ev => Application.Quit()); // Or load a scene

        if (gesturePanel != null) gesturePanel.style.opacity = 0f;
        
        // Initial State
        ShowStartMenu();
    }

    private void StartGame()
    {
        if (startScreen != null) startScreen.style.display = DisplayStyle.None;
        
        // Python'dan gelen işlenmiş görüntüleri almayı başlat
        ToggleWebcam(true);
        
        GameManager.Instance.StartGame();
    }

    public void ToggleWebcam(bool active)
    {
        if (webcamContainer == null) return;
        
        if (active)
        {
            if (FrameReceiver.Instance != null)
                FrameReceiver.Instance.StartReceiving();
            
            webcamContainer.style.display = DisplayStyle.Flex;
        }
        else
        {
            webcamContainer.style.display = DisplayStyle.None;
        }
    }

    public void ShowStartMenu()
    {
        if (startScreen != null) startScreen.style.display = DisplayStyle.Flex;
        if (endScreen != null) endScreen.style.display = DisplayStyle.None;
        Time.timeScale = 0f;
    }

    public void ShowEndMenu(bool victory)
    {
        if (endScreen != null) endScreen.style.display = DisplayStyle.Flex;
        if (endTitle != null)
        {
            endTitle.text = victory ? "VICTORY" : "DEFEAT";
            endTitle.RemoveFromClassList("win-text");
            endTitle.RemoveFromClassList("loss-text");
            endTitle.AddToClassList(victory ? "win-text" : "loss-text");
        }
        if (endMessage != null)
        {
            endMessage.text = victory ? "The waves have been repelled!" : "The castle has fallen...";
        }
    }

    public void FlashGold()
    {
        if (goldText == null) return;
        // Basit bir scale efekti eklenebilir
    }

    private System.Diagnostics.Process pythonProcess;

    private void Start()
    {
        // Her zaman Python'u başlatmaya çalış (Editor'deysen de otomatik açılsın istersen #if kaldırılabilir)
        StartPythonProcess();
    }

    private void StartPythonProcess()
    {
        try
        {
            // Proje kök dizinindeki python_cv/main.py yolunu bul
            string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
            string pythonPath = System.IO.Path.Combine(projectRoot, "python_cv/main.py");
            
            // Eğer venv kullanılıyorsa onun interpreter'ını kullanmak en sağlıklısıdır
            string venvPython = System.IO.Path.Combine(projectRoot, ".venv/Scripts/python.exe");
            
            if (!System.IO.File.Exists(venvPython)) venvPython = "python"; // venv yoksa globale düş

            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = venvPython,
                Arguments = pythonPath,
                UseShellExecute = false,
                CreateNoWindow = true, // Terminal açılmasın, her şey arka planda olsun
                RedirectStandardInput = true // Kapatmak için sinyal gönderebilmek adına
            };

            pythonProcess = System.Diagnostics.Process.Start(startInfo);
            Debug.Log("[UIManager] Python süreci başlatıldı.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Python başlatılamadı: " + e.Message);
        }
    }

    private void OnApplicationQuit()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
            // Process.Kill() bazen hemen sonuçlanmayabilir, garantiye alalım
            pythonProcess.WaitForExit(1000); 
            pythonProcess.Dispose();
            Debug.Log("[UIManager] Python süreci kapatıldı.");
        }
    }

    private void Update()
    {
        // 'V' tuşuna basıldığında kamerayı aç/kapat (New Input System)
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            if (webcamContainer != null)
            {
                bool isVisible = webcamContainer.style.display == DisplayStyle.Flex;
                ToggleWebcam(!isVisible);
            }
        }

        // Python'dan gelen kareleri UI'da güncelle
        if (webcamContainer != null && webcamContainer.style.display == DisplayStyle.Flex)
        {
            if (FrameReceiver.Instance != null && webcamPreview != null)
            {
                FrameReceiver.Instance.UpdateTexture(webcamPreview);
            }
        }

        if (GameManager.Instance != null)
        {
            if (goldText != null) goldText.text = GameManager.Instance.Gold.ToString();
            if (healthText != null) healthText.text = GameManager.Instance.GateHealth.ToString();
            if (playerHealthText != null) playerHealthText.text = GameManager.Instance.PlayerHealth.ToString();
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
