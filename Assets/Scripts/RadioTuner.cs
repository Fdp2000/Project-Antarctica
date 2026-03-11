using UnityEngine;

/// <summary>
/// Phase 2: The Radio Tuning Core
/// Handles the input (tuning dial) and calculates the signal clarity based on 
/// how close the player is to the target frequency.
/// </summary>
public class RadioTuner : MonoBehaviour
{
    [Header("Frequencies")]
    [Range(0f, 100f)]
    public float currentFrequency = 50.0f;
    [Range(0f, 100f)]
    public float targetFrequency = 88.5f;
    
    [Tooltip("How close the dial needs to be to pick up a signal at all.")]
    public float tuningTolerance = 2.0f;

    [Header("Input Setup")]
    [Tooltip("How fast scrolling the wheel or holding the button turns the dial.")]
    public float dialSpeed = 5.0f;
    [Tooltip("The actual physical dial model to rotate.")]
    public Transform dialTransform;
    [Tooltip("The axis on the dial to rotate when tuning.")]
    public Vector3 dialRotationAxis = Vector3.up;

    [Header("Output (Read Only)")]
    [Range(0f, 1f)]
    [Tooltip("0 = pure static. 1 = perfect alignment.")]
    public float signalClarity = 0f;

    void Update()
    {
        HandleTuningInput();
        CalculateSignalClarity();
        AnimateDial();
    }

    private void HandleTuningInput()
    {
        // Simple scroll wheel input for the prototype
        float scrollInput = Input.mouseScrollDelta.y;
        
        if (scrollInput != 0)
        {
            currentFrequency += scrollInput * dialSpeed * Time.deltaTime;
            currentFrequency = Mathf.Clamp(currentFrequency, 0f, 100f);
        }
    }

    private void CalculateSignalClarity()
    {
        float freqDiff = Mathf.Abs(currentFrequency - targetFrequency);

        // If the difference is greater than our tolerance, the signal is 0. 
        // Otherwise, we map it from 0 to 1 based on how close we are.
        signalClarity = Mathf.Clamp01(1.0f - (freqDiff / tuningTolerance));
    }

    private void AnimateDial()
    {
        if (dialTransform != null)
        {
            // Map 0-100 frequency to a 0-360 degree rotation (or whichever scaling fits the model)
            // Just a simple visual mapping: frequency 0 = 0 deg, frequency 100 = 360 deg
            float rotationAngle = (currentFrequency / 100f) * 360f;
            dialTransform.localRotation = Quaternion.Euler(dialRotationAxis * rotationAngle);
        }
    }
}
