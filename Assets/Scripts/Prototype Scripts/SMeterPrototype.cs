using UnityEngine;

public class SMeterPrototype : MonoBehaviour
{
    [Header("Radio Tuning")]
    public float currentFrequency = 88.0f; // Where your dial is currently set
    public float targetFrequency = 94.2f;  // The POI's actual frequency
    public float tuningTolerance = 1.0f;   // How close you need to be to pick up the signal
    public float dialSpeed = 2.0f;         // How fast the scroll wheel turns the dial

    [Header("Targeting")]
    public Transform targetBeacon;
    public float maxSignalRange = 500f;
    public float baselineSignal = 15f;     // The "bump" you get just for finding the frequency!

    [Header("Readout (Read Only)")]
    [Range(0, 100)]
    public float currentSignalStrength = 0f;

    void Update()
    {
        if (targetBeacon == null) return;

        // --- 0. THE DIAL (Mouse Scroll Wheel) ---
        // Scroll up to increase frequency, scroll down to decrease
        float scrollInput = Input.mouseScrollDelta.y;
        if (scrollInput != 0)
        {
            currentFrequency += scrollInput * dialSpeed * Time.deltaTime;
            // Clamp it so it feels like a real radio dial (e.g., 88.0 to 108.0 FM)
            currentFrequency = Mathf.Clamp(currentFrequency, 88.0f, 108.0f);
        }

        // --- 1. TUNING FACTOR ---
        // How far off is our dial from the target?
        float freqDiff = Mathf.Abs(currentFrequency - targetFrequency);

        // 1.0 means perfectly tuned. 0.0 means we are outside the tolerance range.
        float tuningFactor = Mathf.Clamp01(1.0f - (freqDiff / tuningTolerance));

        // --- 2. PROXIMITY ---
        float distance = Vector3.Distance(transform.position, targetBeacon.position);
        float proximityFactor = Mathf.Clamp01(1.0f - (distance / maxSignalRange));

        // --- 3. ALIGNMENT ---
        Vector3 dirToTarget = (targetBeacon.position - transform.position).normalized;
        float alignment = Vector3.Dot(transform.forward, dirToTarget);
        float alignmentFactor = Mathf.Clamp01(alignment);

        // --- 4. THE FINAL CALCULATION ---
        // The spatial signal is now max 85% (because the baseline takes up the first 15%)
        float spatialSignal = proximityFactor * alignmentFactor * (100f - baselineSignal);

        // If tuningFactor is 0 (wrong channel), everything multiplies to 0.
        // If tuning is perfect, you get the baseline PLUS the spatial navigation data!
        currentSignalStrength = tuningFactor * (baselineSignal + spatialSignal);
    }

    void OnGUI()
    {
        // Draw the Radio Frequency Dial
        GUI.color = Color.yellow;
        GUI.skin.label.fontSize = 24;
        GUI.skin.label.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(20, 20, 500, 50), "FREQ: " + currentFrequency.ToString("F2") + " MHz");

        // Draw the S-Meter below it
        GUI.color = Color.green;
        GUI.Label(new Rect(20, 60, 500, 50), "SIGNAL: " + currentSignalStrength.ToString("F1") + " %");
    }
}
