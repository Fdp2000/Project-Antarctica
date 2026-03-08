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
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;

            // 1. Remember the frequency BEFORE we change it
            float previousFrequency = radioScript.currentFrequency;

            // 2. Update and Clamp the Radio Frequency
            radioScript.currentFrequency += mouseX;
            radioScript.currentFrequency = Mathf.Clamp(radioScript.currentFrequency, 88.0f, 108.0f);

            // 3. Calculate how much the frequency ACTUALLY changed
            // If we hit the clamp limit, actualChange will be exactly 0
            float actualChange = radioScript.currentFrequency - previousFrequency;

            // 4. Rotate the physical Knob ONLY by the actual change
            transform.Rotate(Vector3.forward, -actualChange * rotationSpeed * Time.deltaTime);
        }
    }
}