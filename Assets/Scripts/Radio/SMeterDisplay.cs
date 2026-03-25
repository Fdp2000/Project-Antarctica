using UnityEngine;

/// <summary>
/// Phase 2: The Analog Visualizer
/// Controls the physical needle of the S-meter based on the RadioTuner's clarity.
/// Features a capacitor delay, mechanical inertia, analog flutter, and a monster-jamming override.
/// </summary>
public class SMeterDisplay : MonoBehaviour
{
    [Header("Dependencies")]
    public RadioTuner tuner;
    [Tooltip("The actual physical needle GameObject on the dashboard.")]
    public Transform needleTransform;

    [Header("Needle Parameters")]
    [Tooltip("Rotation offset at 0% signal. Keep at 0 if the needle starts in the correct left position!")]
    public float minAngle = 0f;
    [Tooltip("Rotation offset at 100% signal. (Try 90 or -90 to sweep right)")]
    public float maxAngle = 90f;
    [Tooltip("Which axis the needle pivots on. Usually Z (0,0,1) or Y (0,1,0).")]
    public Vector3 rotationAxis = Vector3.forward;

    [Header("Mechanical Feel")]
    [Tooltip("How long (in seconds) it takes the needle to start moving after finding a signal, simulating voltage buildup.")]
    public float reactionDelay = 0.25f;
    [Tooltip("How heavy/sluggish the needle feels. Lower is faster, higher is heavier.")]
    public float needleInertia = 0.15f;
    [Tooltip("Maximum amount of Perlin noise (degrees) added to the needle when signal is weak. Simulates static/flutter.")]
    public float maxFlutterAmount = 5.0f;

    // Internal state variables
    private float targetSignal = 0f;
    private float currentSignal = 0f;
    private float signalVelocity = 0f;

    // Delay Timers
    private float delayTimer = 0f;
    private float previousIncomingSignal = 0f;

    // Encounter Event Override
    private bool isSTheJammingEventActive = false;

    // Safe Rotation Memory
    private Quaternion initialRotation;

    private void Start()
    {
        // Memorize the exact starting angle of the 3D model so we don't break it
        if (needleTransform != null)
        {
            initialRotation = needleTransform.localRotation;
        }

        if (EncounterDirector.Instance != null)
        {
            EncounterDirector.Instance.OnRadioInterferenceStarted += HandleMonsterJamming;
        }

    }

    private void OnDestroy()
    {
        if (EncounterDirector.Instance != null)
        {
            EncounterDirector.Instance.OnRadioInterferenceStarted -= HandleMonsterJamming;
        }
    }

    private void HandleMonsterJamming()
    {
        isSTheJammingEventActive = true;
    }

    private void Update()
    {
        if (tuner == null || needleTransform == null) return;

        UpdateTargetSignal();
        ApplyInertia();
        ApplyFlutterAndRotateNeedle();
    }

    private void UpdateTargetSignal()
    {
        if (isSTheJammingEventActive)
        {
            targetSignal = 1.0f;
            return;
        }

        float incomingSignal = tuner.finalSignalClarity;

        if (Mathf.Abs(incomingSignal - previousIncomingSignal) > 0.05f)
        {
            delayTimer += Time.deltaTime;

            if (delayTimer >= reactionDelay || incomingSignal < 0.01f)
            {
                targetSignal = incomingSignal;
            }
        }
        else
        {
            delayTimer = 0f;
            targetSignal = incomingSignal;
        }

        previousIncomingSignal = incomingSignal;
    }

    private void ApplyInertia()
    {
        currentSignal = Mathf.SmoothDamp(currentSignal, targetSignal, ref signalVelocity, needleInertia);
    }

    private void ApplyFlutterAndRotateNeedle()
    {
        float baseAngle = Mathf.Lerp(minAngle, maxAngle, currentSignal);

        float noiseMultiplier = 1.0f - currentSignal;

        if (isSTheJammingEventActive)
        {
            noiseMultiplier = 2.0f;
        }

        float perlinJitter = (Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f) * 2f;
        float finalAngle = baseAngle + (perlinJitter * maxFlutterAmount * noiseMultiplier);

        // Safely rotate the needle RELATIVE to its starting position
        needleTransform.localRotation = initialRotation * Quaternion.AngleAxis(finalAngle, rotationAxis);
    }
}