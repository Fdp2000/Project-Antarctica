using UnityEngine;

public class KnobInteraction : MonoBehaviour
{
    [Header("Dependencies")]
    public RadioTuner radioScript;
    public SimpleFPSController playerController;

    [Header("Tuning Settings")]
    public float sensitivity = 5.0f;

    [Header("Visuals")]
    public float rotationSpeed = 500f;
    [Tooltip("If the knob spins like a coin instead of a dial, change this to X (1,0,0) or Y (0,1,0)")]
    public Vector3 rotationAxis = Vector3.forward;

    private bool isDragging = false;

    void OnMouseDown()
    {
        isDragging = true;
        // Lock the player's view so the camera doesn't move while tuning
        if (playerController != null) playerController.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnMouseUp()
    {
        ReleaseKnob();
    }

    void Update()
    {
        // --- FAILSAFE ---
        // If the player let go of the left mouse button, but Unity missed the OnMouseUp event, force it to release!
        if (isDragging && !Input.GetMouseButton(0))
        {
            ReleaseKnob();
        }

        if (isDragging && radioScript != null)
        {
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;

            float previousFrequency = radioScript.currentFrequency;

            radioScript.currentFrequency += mouseX;
            radioScript.currentFrequency = Mathf.Clamp(radioScript.currentFrequency, 88.0f, 108.0f);

            float actualChange = radioScript.currentFrequency - previousFrequency;

            // Rotate locally based on the actual change
            transform.Rotate(rotationAxis, -actualChange * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void ReleaseKnob()
    {
        isDragging = false;

        // Re-enable player movement
        if (playerController != null) playerController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}