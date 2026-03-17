using UnityEngine;

public class DoorAlarmLight : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The WinchController that manages the hangar door.")]
    public WinchController winch;

    [Tooltip("The MeshRenderer of the physical bulb model.")]
    public MeshRenderer bulbRenderer;

    [Tooltip("The Point Light sitting inside or next to the bulb.")]
    public Light pointLight;

    [Header("Visuals")]
    [Tooltip("The dull, unlit material (e.g., your 'RedLight' mat without emission).")]
    public Material offMaterial;

    [Tooltip("The bright, glowing material (e.g., a version of 'RedLight' with HDR emission).")]
    public Material onMaterial;

    [Header("Behavior Settings")]
    [Tooltip("If TRUE, the light will flash continuously. If FALSE, it will stay solidly lit.")]
    public bool blinkInsteadOfStatic = true;

    [Tooltip("How fast the alarm flashes (if blinking is enabled).")]
    public float blinkSpeed = 2.0f;

    void Update()
    {
        // If we don't have a winch assigned, just keep the light off and do nothing
        if (winch == null) return;

        // The alarm should be ACTIVE if the door is NOT completely closed
        bool isAlarmActive = !winch.IsDoorClosed;

        if (!isAlarmActive)
        {
            // Door is safely closed. Turn off the alarm.
            SetVisualState(false);
        }
        else
        {
            // Door is open!
            if (!blinkInsteadOfStatic)
            {
                // Solid light mode
                SetVisualState(true);
            }
            else
            {
                // Blinking mode: We use Mathf.Repeat to create a perfect, hard ON/OFF metronome timer
                bool isBlinkCycleOn = Mathf.Repeat(Time.time * blinkSpeed, 1f) > 0.5f;
                SetVisualState(isBlinkCycleOn);
            }
        }
    }

    // A clean helper function to swap the material and toggle the light component at the same time
    private void SetVisualState(bool isOn)
    {
        if (bulbRenderer != null && offMaterial != null && onMaterial != null)
        {
            bulbRenderer.material = isOn ? onMaterial : offMaterial;
        }

        if (pointLight != null)
        {
            pointLight.enabled = isOn;
        }
    }
}