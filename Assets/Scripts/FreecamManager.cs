using UnityEngine;

public class FreecamManager : MonoBehaviour
{
    [Header("Camera Setup")]
    [Tooltip("The main camera attached to your player.")]
    public Camera mainCamera;
    [Tooltip("The CruiserInterior or Fog Overlay camera.")]
    public Camera overlayCamera;
    [Tooltip("The GameObject containing your NoClipCamera script.")]
    public GameObject freecamObject;

    [Header("UI Setup")]
    [Tooltip("Drag your CrossHair canvas object here.")]
    public GameObject crosshairUI;

    [Header("Controls")]
    [Tooltip("Key to switch between Player and Freecam.")]
    public KeyCode toggleFreecamKey = KeyCode.F10;
    [Tooltip("Key to hide/show the crosshair.")]
    public KeyCode toggleCrosshairKey = KeyCode.F11;

    private bool isFreecamActive = false;
    private FogBypass fogBypassScript;

    void Start()
    {
        // Ensure freecam is turned off when the game starts
        if (freecamObject != null) freecamObject.SetActive(false);

        // Find your FogBypass script so we can pause it later
        fogBypassScript = FindObjectOfType<FogBypass>();
    }

    void Update()
    {
        // Toggle the Freecam
        if (Input.GetKeyDown(toggleFreecamKey))
        {
            ToggleFreecam();
        }

        // Toggle the Crosshair UI
        if (Input.GetKeyDown(toggleCrosshairKey))
        {
            if (crosshairUI != null)
            {
                // Flips the active state: if it's on, turn it off. If it's off, turn it on!
                crosshairUI.SetActive(!crosshairUI.activeSelf);
            }
        }
    }

    private void ToggleFreecam()
    {
        isFreecamActive = !isFreecamActive;

        if (isFreecamActive)
        {
            // 1. TELEPORT: Match the Freecam to the Main Camera's exact position and rotation
            if (mainCamera != null && freecamObject != null)
            {
                freecamObject.transform.position = mainCamera.transform.position;
                freecamObject.transform.rotation = mainCamera.transform.rotation;
            }

            // 2. PAUSE CONFLICTS: Stop the FogBypass script from fighting us
            if (fogBypassScript != null) fogBypassScript.enabled = false;

            // 3. DISABLE PLAYER CAMS: Turn off the Camera components (not the GameObjects)
            if (mainCamera != null)
            {
                mainCamera.enabled = false;

                // Turn off the Audio Listener to prevent Unity console spam
                AudioListener al = mainCamera.GetComponent<AudioListener>();
                if (al != null) al.enabled = false;
            }

            if (overlayCamera != null) overlayCamera.enabled = false;

            // 4. ACTIVATE FREECAM: Turn on the noclip object
            if (freecamObject != null) freecamObject.SetActive(true);
        }
        else
        {
            // 1. DISABLE FREECAM
            if (freecamObject != null) freecamObject.SetActive(false);

            // 2. RE-ENABLE PLAYER CAMS
            if (mainCamera != null)
            {
                mainCamera.enabled = true;

                AudioListener al = mainCamera.GetComponent<AudioListener>();
                if (al != null) al.enabled = true;
            }

            // 3. RE-ENABLE CONFLICTS: Turn FogBypass back on
            if (fogBypassScript != null) fogBypassScript.enabled = true;
        }
    }
}