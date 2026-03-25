using UnityEngine;

public class FinalScienceKnobInteraction : MonoBehaviour
{
    public FinalCRTWaveController waveController;
    public SimpleFPSController playerController;
    public enum KnobType { Amplitude, Frequency, Phase }
    public KnobType function;
    public float sensitivity = 1.5f;
    public Vector3 rotationAxis = Vector3.forward;
    public float minVisualAngle = -135f, maxVisualAngle = 135f;

    private bool isDragging = false;
    private Quaternion baseRotation;

    void Start() { baseRotation = transform.localRotation; }

    public void StartManualDrag()
    {
        isDragging = true;
        if (playerController != null) playerController.enabled = false;
    }

    void Update()
    {
        if (isDragging)
        {
            if (!Input.GetKey(playerController.interactKey) && !Input.GetMouseButton(0))
            {
                isDragging = false;
                if (playerController != null) playerController.enabled = true;
                return;
            }

            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            switch (function)
            {
                case KnobType.Amplitude: waveController.playerAmplitude = Mathf.Clamp(waveController.playerAmplitude + (mouseX * 0.01f), 0.16f, 1.1f); break;
                case KnobType.Frequency: waveController.playerFrequency = Mathf.Clamp(waveController.playerFrequency + (mouseX * 0.1f), 6.2f, 10.0f); break;
                case KnobType.Phase: waveController.playerPhase = Mathf.Clamp(waveController.playerPhase + (mouseX * 0.2f), 0f, 12.56f); break;
            }

            float val = 0, min = 0, max = 1;
            if (function == KnobType.Amplitude) { val = waveController.playerAmplitude; min = 0.16f; max = 1.1f; }
            else if (function == KnobType.Frequency) { val = waveController.playerFrequency; min = 6.2f; max = 10.0f; }
            else { val = waveController.playerPhase; min = 0f; max = 12.56f; }

            float t = Mathf.InverseLerp(min, max, val);
            transform.localRotation = baseRotation * Quaternion.AngleAxis(Mathf.Lerp(minVisualAngle, maxVisualAngle, t), rotationAxis);
        }
    }
}