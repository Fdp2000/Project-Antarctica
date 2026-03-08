using UnityEngine;

public class ScienceKnobInteraction : MonoBehaviour
{
    [Header("References")]
    public SeismographDrum drumScript;
    public SimpleFPSController playerController;

    public enum KnobType { Amplitude, Frequency, Phase }
    [Header("Knob Function")]
    public KnobType function;

    [Header("Testing")]
    public bool debugWithoutSeating = false; // Set to true to test without sitting in the chair

    [Header("Settings")]
    public float sensitivity = 1.5f;
    public float rotationVisualSpeed = 500f;

    private bool isDragging = false;

    void OnMouseDown()
    {
        // Check for seat status or debug override
        if (debugWithoutSeating || (playerController != null && playerController.isDoingScience))
        {
            isDragging = true;
            if (playerController != null) playerController.enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;
            if (playerController != null) playerController.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (isDragging && drumScript != null)
        {
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            float actualChange = 0f;

            switch (function)
            {
                case KnobType.Amplitude:
                    float prevAmp = drumScript.playerAmplitude;
                    drumScript.playerAmplitude = Mathf.Clamp(drumScript.playerAmplitude + (mouseX * 0.01f), 0.02f, 0.2f);
                    actualChange = drumScript.playerAmplitude - prevAmp;
                    break;

                case KnobType.Frequency:
                    float prevFreq = drumScript.playerFrequency;
                    drumScript.playerFrequency = Mathf.Clamp(drumScript.playerFrequency + (mouseX * 0.1f), 0.5f, 6.0f);
                    actualChange = drumScript.playerFrequency - prevFreq;
                    break;

                case KnobType.Phase:
                    // Phase is infinite, so we use raw input for visual rotation
                    drumScript.playerPhase += mouseX * 0.1f;
                    actualChange = mouseX;
                    break;
            }

            // Rotate visual knob model
            if (actualChange != 0)
            {
                transform.Rotate(Vector3.forward, -mouseX * rotationVisualSpeed * Time.deltaTime);
            }
        }
    }
}