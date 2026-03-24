using UnityEngine;

public class RadioTuner : MonoBehaviour
{
    [Header("Dependencies")]
    public Transform vehicleTransform;
    [Tooltip("Drag all the Radio Beacons in your scene into this array.")]
    public RadioBeacon[] availableBeacons;

    [Header("Tuning Settings")]
    [Tooltip("Standard FM Broadcast Band")]
    [Range(88f, 108f)]
    public float currentFrequency = 88.0f;
    public float tuningTolerance = 2.0f;
    public float jitterSpeed = 2.0f;

    [Header("Signal Weights")]
    [Range(0f, 1f)] public float baseTuningWeight = 0.15f;
    [Range(0f, 1f)] public float directionWeight = 0.30f;
    [Range(0f, 1f)] public float distanceWeight = 0.55f;

    [Header("Navigation Settings")]
    public float maxDirectionalAngle = 60f;
    public float maxSignalDistance = 500f;
    [Range(0f, 1f)] public float directionInfluenceOnDistance = 0.8f;

    [Tooltip("How many meters outside the proximity radius it takes to smoothly fade back to normal distance math.")]
    public float proximityBlendDistance = 20f; // NEW: The physical crossfade zone

    [Header("Transitions & Smoothing")]
    [Tooltip("How fast the OVERALL signal glides up or down. Set to 0 for instant.")]
    public float signalTransitionSpeed = 2.0f;

    [Header("Tuning Speed Penalty (Failsafe)")]
    [Tooltip("Maximum MHz per second the dial can move before the signal drops.")]
    public float maxTuningSpeed = 8.0f;
    [Tooltip("How fast the signal recovers (fades back in) after they stop violently tuning.")]
    public float signalRecoverySpeed = 1.5f;

    private float previousFrequency;
    private float tuningPenalty = 0f;

    [Header("Output (Read Only)")]
    [Range(0f, 1f)]
    public float finalSignalClarity;

    [HideInInspector]
    public RadioBeacon activeBeacon;

    [Header("Debug")]
    public bool showDebugUI = true;
    public bool showFrequencyUI = true;
    public bool showSignalUI = true;

    void Start()
    {
        previousFrequency = currentFrequency;
    }

    void Update()
    {
        float currentTuningSpeed = Mathf.Abs(currentFrequency - previousFrequency) / Time.deltaTime;
        previousFrequency = currentFrequency;

        if (currentTuningSpeed > maxTuningSpeed)
        {
            tuningPenalty = 1f;
        }
        else
        {
            tuningPenalty = Mathf.MoveTowards(tuningPenalty, 0f, signalRecoverySpeed * Time.deltaTime);
        }

        if (availableBeacons == null || availableBeacons.Length == 0 || vehicleTransform == null)
        {
            ApplyFinalSignal(0f);
            return;
        }

        float highestSignalThisFrame = 0f;
        RadioBeacon strongestBeacon = null;

        foreach (RadioBeacon beacon in availableBeacons)
        {
            // --- THE FIX: Ignore empty slots AND disabled POIs ---
            if (beacon == null || !beacon.gameObject.activeInHierarchy) continue;

            float beaconSignal = EvaluateBeaconSignal(beacon);

            if (beaconSignal > highestSignalThisFrame)
            {
                highestSignalThisFrame = beaconSignal;
                strongestBeacon = beacon;
            }
        }

        activeBeacon = strongestBeacon;
        ApplyFinalSignal(highestSignalThisFrame);
    }

    private float EvaluateBeaconSignal(RadioBeacon beacon)
    {
        if (beacon.isDead) return 0f;

        float freqDiff = Mathf.Abs(currentFrequency - beacon.broadcastFrequency);
        float rawTuning = Mathf.Clamp01(1.0f - (freqDiff / tuningTolerance));

        if (rawTuning <= 0.01f) return 0f;

        float jitter = (Mathf.PerlinNoise(Time.time * jitterSpeed, beacon.GetInstanceID()) - 0.5f) * 0.04f;
        float activeTuning = Mathf.Clamp01(rawTuning + jitter);

        // --- THE UNIFIED OVERWRITE LOGIC ---
        if (beacon.isCompleted)
        {
            float lockedMaxSignal = activeTuning * (baseTuningWeight + directionWeight + distanceWeight);
            return lockedMaxSignal * beacon.signalMultiplier;
        }

        // --- NORMAL NAVIGATION MATH ---
        Vector3 dirToBeacon = (beacon.transform.position - vehicleTransform.position).normalized;
        dirToBeacon.y = 0;
        Vector3 vehicleForward = vehicleTransform.forward;
        vehicleForward.y = 0;

        float angleToBeacon = Vector3.Angle(vehicleForward, dirToBeacon);
        float directionScore = Mathf.Clamp01(1.0f - (angleToBeacon / maxDirectionalAngle));

        float currentDistance = Vector3.Distance(vehicleTransform.position, beacon.transform.position);

        float distanceScore = Mathf.InverseLerp(maxSignalDistance, beacon.proximityRadius, currentDistance);

        float effectiveDistanceMultiplier = (1.0f - directionInfluenceOnDistance) + (directionScore * directionInfluenceOnDistance);

        float standardSignal = (activeTuning * baseTuningWeight) +
                               (activeTuning * directionScore * directionWeight) +
                               (activeTuning * effectiveDistanceMultiplier * distanceScore * distanceWeight);

        float overrideSignal = activeTuning * (baseTuningWeight + directionWeight + distanceWeight);

        // --- NEW: THE BLEND ZONE MATH ---
        // Instead of instantly snapping from 1 to 0, it physically crossfades over the 'proximityBlendDistance'
        float proximityWeight = Mathf.InverseLerp(beacon.proximityRadius + proximityBlendDistance, beacon.proximityRadius, currentDistance);

        return Mathf.Lerp(standardSignal, overrideSignal, proximityWeight);
    }

    private void ApplyFinalSignal(float targetSignal)
    {
        float penalizedSignal = targetSignal * (1f - tuningPenalty);

        if (signalTransitionSpeed > 0f)
        {
            finalSignalClarity = Mathf.MoveTowards(finalSignalClarity, penalizedSignal, signalTransitionSpeed * Time.deltaTime);
        }
        else
        {
            finalSignalClarity = penalizedSignal;
        }
    }

    void OnGUI()
    {
        if (!showDebugUI) return;

        int currentYPosition = 20;

        if (showFrequencyUI)
        {
            GUI.color = Color.yellow;
            GUI.skin.label.fontSize = 24;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(20, currentYPosition, 500, 50), "FREQ: " + currentFrequency.ToString("F1") + " MHz");

            currentYPosition += 40;
        }

        if (showSignalUI)
        {
            GUI.color = Color.green;
            GUI.skin.label.fontSize = 24;
            GUI.skin.label.fontStyle = FontStyle.Bold;

            float signalPercentage = finalSignalClarity * 100f;
            GUI.Label(new Rect(20, currentYPosition, 500, 50), "SIGNAL: " + signalPercentage.ToString("F0") + " %");

            currentYPosition += 40;

            if (activeBeacon != null && finalSignalClarity > 0.01f)
            {
                GUI.color = Color.cyan;
                GUI.Label(new Rect(20, currentYPosition, 500, 50), "LOCKED ON: " + activeBeacon.gameObject.name);
            }
        }
    }
}