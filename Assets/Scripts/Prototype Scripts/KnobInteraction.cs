using UnityEngine;

public class KnobInteraction : MonoBehaviour
{
    public SMeterPrototype radioScript;
    public SimpleFPSController playerController; // NEW: Drag your Player here
    public float sensitivity = 5.0f;
    public float rotationSpeed = 500f;


    private bool isDragging = false;

    void OnMouseDown()
    {
        isDragging = true;
        // Lock the player's view so the camera doesn't move
        if (playerController != null) playerController.enabled = false;

        // KEEP IT HIDDEN: Lock the cursor to the center and keep it invisible
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnMouseUp()
    {
        isDragging = false;
        // Re-enable player movement
        if (playerController != null) playerController.enabled = true;

        // Keep it locked for standard FPS gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (isDragging && radioScript != null)
        {
            // Use GetAxis("Mouse X") instead of raw mousePosition. 
            // This is much smoother for "dragging" interactions.
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;

            // 1. Update Radio Logic
            radioScript.currentFrequency += mouseX;
            radioScript.currentFrequency = Mathf.Clamp(radioScript.currentFrequency, 88.0f, 108.0f);

            // 2. Rotate the physical Knob (Local Z is usually the 'spin' axis for knobs)
            transform.Rotate(Vector3.forward, -mouseX * rotationSpeed * Time.deltaTime);
        }
    }
}