using UnityEngine;

public class SensitivityManager : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Drag your player object here.")]
    public SimpleFPSController playerController;

    [Header("Tuning Settings")]
    public float minSensitivity = 0.5f;
    public float maxSensitivity = 5.0f;
    [Tooltip("Industry standard is 0.1 for precise PC tuning.")]
    public float sensitivityStep = 0.1f;

    [Header("UI Feedback")]
    public float displayDuration = 2.0f;
    private float displayTimer = 0f;
    private string displayText = "";

    // Stores your custom hex color
    private Color customGreen;

    void Start()
    {
        if (playerController == null)
            playerController = FindObjectOfType<SimpleFPSController>();

        // Convert your specific Hex code into a Unity Color automatically
        ColorUtility.TryParseHtmlString("#1BF100", out customGreen);
    }

    void Update()
    {
        if (playerController == null) return;

        bool hasChanged = false;

        // --- THE INVISIBLE MENU HOTKEYS ---
        if (Input.GetKeyDown(KeyCode.I))
        {
            playerController.lookSensitivity += sensitivityStep;
            hasChanged = true;
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            playerController.lookSensitivity -= sensitivityStep;
            hasChanged = true;
        }

        // --- SAVE AND DISPLAY ---
        if (hasChanged)
        {
            playerController.lookSensitivity = Mathf.Clamp(playerController.lookSensitivity, minSensitivity, maxSensitivity);

            PlayerPrefs.SetFloat("PlayerSensitivity", playerController.lookSensitivity);
            PlayerPrefs.Save();

            displayText = "Sensitivity: " + playerController.lookSensitivity.ToString("F1");
            displayTimer = displayDuration;
        }

        if (displayTimer > 0)
        {
            displayTimer -= Time.deltaTime;
        }
    }

    void OnGUI()
    {
        if (displayTimer > 0)
        {
            // Smoothly fade the text out using the alpha channel
            float alpha = Mathf.Clamp01(displayTimer);
            customGreen.a = alpha;

            // Apply your custom hex color!
            GUI.color = customGreen;

            GUI.skin.label.fontSize = 24;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.alignment = TextAnchor.LowerRight;

            GUI.Label(new Rect(Screen.width - 250, Screen.height - 60, 230, 50), displayText);
        }
    }
}