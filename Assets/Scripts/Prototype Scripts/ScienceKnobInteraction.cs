using UnityEngine;

public class ScienceKnobInteraction : MonoBehaviour
{
    [Header("References")]
    public CRTWaveController waveController;
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
        if (isDragging && waveController != null)
        {
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            float actualChange = 0f;

            switch (function)
            {
                case KnobType.Amplitude:
                    float prevAmp = waveController.playerAmplitude;
                    // Lowered clamp max to 0.4f to match the new visual scale
                    waveController.playerAmplitude = Mathf.Clamp(waveController.playerAmplitude + (mouseX * 0.01f), 0.01f, 0.4f);
                    actualChange = waveController.playerAmplitude - prevAmp;
                    break;

                case KnobType.Frequency:
                    float prevFreq = waveController.playerFrequency;
                    waveController.playerFrequency = Mathf.Clamp(waveController.playerFrequency + (mouseX * 0.1f), 0.1f, 10.0f);
                    actualChange = waveController.playerFrequency - prevFreq;
                    break;

                case KnobType.Phase:
                    float prevPhase = waveController.playerPhase;
                    waveController.playerPhase += mouseX * 0.2f; // Unclamped or looped so they can twist it endlessly to catch up
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