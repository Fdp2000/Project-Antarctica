using UnityEngine;

public class WebGLFPSCounter : MonoBehaviour
{
    [Header("Secret Key Combo")]
    public KeyCode holdKey = KeyCode.LeftControl;
    public KeyCode pressKey = KeyCode.F;

    private bool showFPS = false;
    private float deltaTime = 0.0f;

    void Update()
    {
        // Toggle visibility when both keys are pressed
        if (Input.GetKey(holdKey) && Input.GetKeyDown(pressKey))
        {
            showFPS = !showFPS;
        }

        // Calculate frame rate smoothly
        if (showFPS)
        {
            // This creates a smoothed average rather than a rapidly flickering number
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }
    }

    void OnGUI()
    {
        if (!showFPS) return;

        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();
        Rect rect = new Rect(10, 10, w, h * 2 / 100);

        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = Mathf.Clamp(h * 2 / 100, 24, 48); // Keeps text readable

        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        string text = string.Format("{0:0.0} ms ({1:0.} FPS)", msec, fps);

        // --- Color Logic ---
        if (fps >= 60) style.normal.textColor = Color.green;
        else if (fps >= 30) style.normal.textColor = Color.yellow;
        else style.normal.textColor = Color.red;

        // Draw a black drop-shadow first so it's readable against the bright white snow!
        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = Color.black;
        Rect shadowRect = new Rect(12, 12, w, h * 2 / 100);

        GUI.Label(shadowRect, text, shadowStyle);
        GUI.Label(rect, text, style);
    }
}