using UnityEngine;

public class RadioTuner : MonoBehaviour
{
    [Header("Dependencies")]
    public Transform vehicleTransform;
    public RadioBeacon targetBeacon;

    [Header("Tuning Settings")]
    [Range(0f, 100f)] public float currentFrequency = 88.5f;
    public float tuningTolerance = 2.0f;
    public float jitterSpeed = 2.0f;

    [Header("Signal Weights")]
    [Range(0f, 1f)] public float baseTuningWeight = 0.15f;
    [Range(0f, 1f)] public float directionWeight = 0.30f;
    [Range(0f, 1f)] public float distanceWeight = 0.55f;

    [Header("Navigation Settings")]
    public float maxDirectionalAngle = 60f;
    public float maxSignalDistance = 500f;
    public float proximityZoneRadius = 50f;
    [Range(0f, 1f)] public float directionInfluenceOnDistance = 0.8f;

    [Header("Transitions & Smoothing")]
    [Tooltip("How many seconds it takes to crossfade when entering/exiting the proximity bubble.")]
    public float proximityTransitionTime = 1.0f;

    [Tooltip("How fast the OVERALL signal glides up or down (e.g., 0.25 = 4 seconds to climb 100%). Set to 0 for instant.")]
    public float signalTransitionSpeed = 0f;

    [Header("Output (Read Only)")]
    [Range(0f, 1f)]
    public float finalSignalClarity;

    [Header("Debug")]
    public bool showDebugUI = true;

    // Internal variable to track the crossfade between standard math and proximity math
    private float currentProximityWeight = 0f;

    void Update()
    {
        if (targetBeacon == null || vehicleTransform == null) return;

        CalculateSignal();
    }

    private void CalculateSignal()
    {
        // 1. THE BASE TUNING & JITTER (Instant)
        float freqDiff = Mathf.Abs(currentFrequency - targetBeacon.broadcastFrequency);
        float rawTuning = Mathf.Clamp01(1.0f - (freqDiff / tuningTolerance));

        if (rawTuning <= 0.01f)
        {
            ApplyFinalSignal(0f);
            return;
        }

        float jitter = (Mathf.PerlinNoise(Time.time * jitterSpeed, 0f) - 0.5f) * 0.04f;
        float activeTuning = Mathf.Clamp01(rawTuning + jitter);

        // 2. DIRECTIONAL ALLIGNMENT (Instant)
        Vector3 dirToBeacon = (targetBeacon.transform.position - vehicleTransform.position).normalized;
        dirToBeacon.y = 0;
        Vector3 vehicleForward = vehicleTransform.forward;
        vehicleForward.y = 0;

        float angleToBeacon = Vector3.Angle(vehicleForward, dirToBeacon);
        float directionScore = Mathf.Clamp01(1.0f - (angleToBeacon / maxDirectionalAngle));

        // 3. DISTANCE ALLIGNMENT (Instant)
        float currentDistance = Vector3.Distance(vehicleTransform.position, targetBeacon.transform.position);
        float distanceScore = Mathf.InverseLerp(maxSignalDistance, proximityZoneRadius, currentDistance);

        // 4. CALCULATE BOTH STATES
        // State A: Outside the bubble (Standard Math)
        float effectiveDistanceMultiplier = (1.0f - directionInfluenceOnDistance) + (directionScore * directionInfluenceOnDistance);
        float standardSignal = (activeTuning * baseTuningWeight) +
                               (activeTuning * directionScore * directionWeight) +
                               (activeTuning * effectiveDistanceMultiplier * distanceScore * distanceWeight);

        // State B: Inside the bubble (Override Math)
        float overrideSignal = activeTuning * (baseTuningWeight + directionWeight + distanceWeight);

        // 5. THE PROXIMITY LERP
        float targetProximityWeight = (currentDistance <= proximityZoneRadius) ? 1f : 0f;

        if (proximityTransitionTime > 0f)
        {
            float transitionSpeed = 1f / proximityTransitionTime;
            currentProximityWeight = Mathf.MoveTowards(currentProximityWeight, targetProximityWeight, transitionSpeed * Time.deltaTime);
        }
        else
        {
            currentProximityWeight = targetProximityWeight;
        }

        // Crossfade to get the target signal for this exact frame
        float targetSignal = Mathf.Lerp(standardSignal, overrideSignal, currentProximityWeight);

        // 6. APPLY OVERALL TRANSITION SPEED
        ApplyFinalSignal(targetSignal);
    }

    private void ApplyFinalSignal(float targetSignal)
    {
        if (signalTransitionSpeed > 0f)
        {
            finalSignalClarity = Mathf.MoveTowards(finalSignalClarity, targetSignal, signalTransitionSpeed * Time.deltaTime);
        }
        else
        {
            finalSignalClarity = targetSignal;
        }
    }

    void OnGUI()
    {
        if (!showDebugUI) return;

        GUI.color = Color.yellow;
        GUI.skin.label.fontSize = 24;
        GUI.skin.label.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(20, 20, 500, 50), "FREQ: " + currentFrequency.ToString("F1") + " MHz");

        GUI.color = Color.green;
        float signalPercentage = finalSignalClarity * 100f;
        GUI.Label(new Rect(20, 60, 500, 50), "SIGNAL: " + signalPercentage.ToString("F0") + " %");
    }
}