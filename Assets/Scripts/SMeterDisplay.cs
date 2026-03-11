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
    [Tooltip("Z-Rotation of the needle at 0% signal (Far Left).")]
    public float minAngle = -45f;
    [Tooltip("Z-Rotation of the needle at 100% signal (Far Right).")]
    public float maxAngle = 45f;

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
    private float signalVelocity = 0f; // Used by SmoothDamp
    
    // Delay Timers
    private float delayTimer = 0f;
    private float previousIncomingSignal = 0f;

    // Encounter Event Override
    private bool isSTheJammingEventActive = false;

    private void Start()
    {
        // Subscribe to exactly the event requested in the prompt
        if (EncounterDirector.Instance != null)
        {
            EncounterDirector.Instance.OnRadioInterferenceStarted += HandleMonsterJamming;
        }
        else
        {
            Debug.LogWarning("SMeterDisplay: EncounterDirector not found in scene. Monster jamming event won't trigger.");
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
        // When the monster event triggers, we permanently (or until reversed) peg the meter to max and turn on max flutter
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
            // Monster Event: Ignore the tuner, force to 100%
            targetSignal = 1.0f;
            return;
        }

        // Get the real signal from the physical tuner dial
        float incomingSignal = tuner.signalClarity;

        // --- The Capacitor Delay ---
        // If the signal suddenly changes (like scrolling onto the target frequency), 
        // start counting the delay timer. If the user scrolls past it too fast, it won't trigger.
        if (Mathf.Abs(incomingSignal - previousIncomingSignal) > 0.05f) 
        {
            delayTimer += Time.deltaTime;
            
            // If the delay has passed, OR if the signal dropped to 0 (power lost instantly), set the target
            if (delayTimer >= reactionDelay || incomingSignal < 0.01f)
            {
                targetSignal = incomingSignal;
            }
        }
        else
        {
            // If the signal is stable, reset the timer
            delayTimer = 0f;
            targetSignal = incomingSignal;
        }

        previousIncomingSignal = incomingSignal;
    }

    private void ApplyInertia()
    {
        // --- Mechanical Inertia ---
        // SmoothDamp simulates the physical "weight" of the metal needle accelerating and decelerating
        currentSignal = Mathf.SmoothDamp(currentSignal, targetSignal, ref signalVelocity, needleInertia);
    }

    private void ApplyFlutterAndRotateNeedle()
    {
        // Calculate the base target angle based on 0-1 percentage between Min and Max
        float baseZAngle = Mathf.Lerp(minAngle, maxAngle, currentSignal);

        // --- Analog Flutter (Perlin Noise) ---
        // We want HIGH flutter when signal is LOW (0.0), and smoothly lock to LOW flutter when signal is HIGH (1.0).
        // 1.0 - currentSignal gives us that inverse relationship.
        float noiseMultiplier = 1.0f - currentSignal;
        
        // However, if the monster is jamming the radio, we force the highest flutter possible even at 100% signal!
        if (isSTheJammingEventActive)
        {
            noiseMultiplier = 2.0f; // Double flutter for extra chaos during an encounter
        }

        // Generate the perlin jitter
        float perlinJitter = (Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f) * 2f; // -1 to +1 space
        float finalZAngle = baseZAngle + (perlinJitter * maxFlutterAmount * noiseMultiplier);

        // Finally, rotate the needle! Assuming Z is the up-axis.
        needleTransform.localRotation = Quaternion.Euler(0f, 0f, finalZAngle);
    }
}
