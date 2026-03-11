using UnityEngine;

/// <summary>
/// Phase 2: The Radio Tuning Core
/// Retrieves the frequency value set by the knob mechanic and calculates the signal clarity based on 
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

    [Header("Output (Read Only)")]
    [Range(0f, 1f)]
    [Tooltip("0 = pure static. 1 = perfect alignment.")]
    public float signalClarity = 0f;

    void Update()
    {
        CalculateSignalClarity();
    }

    private void CalculateSignalClarity()
    {
        float freqDiff = Mathf.Abs(currentFrequency - targetFrequency);

        // If the difference is greater than our tolerance, the signal is 0. 
        // Otherwise, we map it from 0 to 1 based on how close we are.
        signalClarity = Mathf.Clamp01(1.0f - (freqDiff / tuningTolerance));
    }
}
