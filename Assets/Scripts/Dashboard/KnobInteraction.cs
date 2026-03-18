using UnityEngine;

public class KnobInteraction : MonoBehaviour
{
    public RadioTuner radioScript;
    public SimpleFPSController playerController;
    public float sensitivity = 5.0f;
    public Vector3 rotationAxis = Vector3.forward;
    public float minVisualAngle = -150f, maxVisualAngle = 150f;

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
            // Stop if they let go of E or Mouse
            if (!Input.GetKey(playerController.interactKey) && !Input.GetMouseButton(0))
            {
                isDragging = false;
                if (playerController != null) playerController.enabled = true;
                return;
            }

            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            radioScript.currentFrequency = Mathf.Clamp(radioScript.currentFrequency + mouseX, 88.0f, 108.0f);

            float t = Mathf.InverseLerp(88.0f, 108.0f, radioScript.currentFrequency);
            transform.localRotation = baseRotation * Quaternion.AngleAxis(Mathf.Lerp(minVisualAngle, maxVisualAngle, t), rotationAxis);
        }
    }
}