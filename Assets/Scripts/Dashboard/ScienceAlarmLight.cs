using UnityEngine;

public class ScienceAlarmLight : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The receiver to check if a tape is currently inserted.")]
    public CassetteReceiver receiver;

    [Tooltip("The MeshRenderer of the physical bulb model.")]
    public MeshRenderer bulbRenderer;

    [Tooltip("The Point Light sitting inside or next to the bulb.")]
    public Light pointLight;

    [Header("Visuals")]
    public Material offMaterial;
    public Material onMaterial;

    [Header("Behavior Settings")]
    [Tooltip("If TRUE, the light will flash continuously. If FALSE, it will stay solidly lit.")]
    public bool blinkInsteadOfStatic = true;
    public float blinkSpeed = 2.0f;

    void Update()
    {
        if (receiver == null) return;

        // The alarm should be ACTIVE if a tape is inside AND the POI is not fully completed yet
        bool isAlarmActive = false;

        if (receiver.hasCassette && receiver.currentlyInsertedBeacon != null)
        {
            // If the beacon isn't marked as complete (meaning the punchcard hasn't been collected)
            if (!receiver.currentlyInsertedBeacon.isCompleted)
            {
                isAlarmActive = true;
            }
        }

        if (!isAlarmActive)
        {
            SetVisualState(false);
        }
        else
        {
            if (!blinkInsteadOfStatic)
            {
                SetVisualState(true);
            }
            else
            {
                bool isBlinkCycleOn = Mathf.Repeat(Time.time * blinkSpeed, 1f) > 0.5f;
                SetVisualState(isBlinkCycleOn);
            }
        }
    }

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