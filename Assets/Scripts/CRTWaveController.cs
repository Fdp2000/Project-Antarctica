using UnityEngine;

public class CRTWaveController : MonoBehaviour
{
    [Header("Line Renderers")]
    [Tooltip("The Amber target wave that the player tries to match.")]
    public LineRenderer targetLine;
    [Tooltip("The Green player wave controlled by the physical knobs.")]
    public LineRenderer playerLine;

    [Header("Minigame Logic/Feedback")]
    [Tooltip("Check this to force the bulb ON for testing purposes.")]
    public bool debugForceSync = false;
    
    [Tooltip("The MeshRenderer of the physical light bulb model.")]
    public MeshRenderer lightRenderer;
    [Tooltip("An actual Unity Light component GameObject to turn on/off.")]
    public GameObject pointLightObject;
    
    [Tooltip("The 3 small progress LEDs. Order them Bottom (Index 0) to Top (Index 2).")]
    public MeshRenderer[] progressLEDs = new MeshRenderer[3];
    [Tooltip("The 3 Unity Point Lights accompanying the LEDs. Order them matching the renderers.")]
    public Light[] progressPointLights = new Light[3];
    [Tooltip("The maximum intensity the point light should reach when its chunk of progress is complete.")]
    public float maxProgressLightIntensity = 1.0f;
    
    [Tooltip("The material to use when the waves are OUT of sync (Off).")]
    public Material lightOffMaterial;
    [Tooltip("The material to use when the waves are IN sync (Emissive/On).")]
    public Material lightOnMaterial;
    
    [Tooltip("How long (in continuous seconds) the player must remain in sync to fully light all 3 LEDs and win.")]
    public float timeToComplete = 6.0f;
    
    [Header("Rewards")]
    [Tooltip("The GameObject prefab to spawn when the player wins (the punchcard).")]
    public GameObject punchcardPrefab;
    [Tooltip("Where the punchcard should physically appear on the machine.")]
    public Transform punchcardSpawnPoint;
    
    [Tooltip("How close the player's knobs must be to the drifting target math.")]
    public float matchTolerance = 0.2f;

    [Header("Wave Rendering Settings")]
    [Tooltip("Number of points on the line. Higher = smoother curve.")]
    public int numPoints = 200;
    [Tooltip("Total width of the wave rendered across the screen.")]
    public float waveWidth = 0.65f;
    [Tooltip("How fast the wave 'crawls' horizontally across the screen.")]
    public float runSpeed = -2f;
    [Tooltip("Visual multiplier for frequency to ensure multiple peaks fit on screen.")]
    public float visualDensity = 5f;

    [Header("Player Knobs (Inputs)")]
    // These floats act as the 'hooks' for the physical knob scripts to modify
    [Range(0.01f, 0.4f)] public float playerAmplitude = 0.15f;
    [Range(0.1f, 10f)] public float playerFrequency = 2f;
    [Range(-10f, 10f)] public float playerPhase = 0f;

    [Header("Target Wave Settings (Amber)")]
    public float baseTargetAmplitude = 0.15f;
    public float baseTargetFrequency = 2f;
    public float baseTargetPhase = 0f;
    
    [Header("Target Target Drift (Timer Based)")]
    [Tooltip("How many seconds the target waves hold steady before moving again.")]
    public float driftInterval = 4.0f;
    [Tooltip("How long it takes to lerp from the old target to the new target.")]
    public float driftLerpDuration = 1.5f;

    [Tooltip("How far the target amplitude can wander from its base value.")]
    public float amplitudeDriftVariance = 0.1f;
    [Tooltip("How far the target frequency can wander from its base value.")]
    public float frequencyDriftVariance = 1.0f;
    [Tooltip("How far the target phase can wander from its base value.")]
    public float phaseDriftVariance = 2.0f;

    // Internal variables for the actual drawn values
    private float currentTargetAmplitude;
    private float currentTargetFrequency;
    private float currentTargetPhase;

    // Variables for the Lerping system
    private float oldTargetAmplitude;
    private float oldTargetFrequency;
    private float oldTargetPhase;

    private float newTargetAmplitude;
    private float newTargetFrequency;
    private float newTargetPhase;

    private float timer = 0f;
    private float lerpTimer = 0f;
    
    // Progress Math
    [HideInInspector] public float currentProgress = 0f;
    [HideInInspector] public bool isMinigameComplete = false;

    void Start()
    {
        // Initialize the LineRenderers
        if (targetLine) targetLine.positionCount = numPoints;
        if (playerLine) playerLine.positionCount = numPoints;

        // Ensure lines draw relative to this object's transform so it moves with the screen
        if (targetLine) targetLine.useWorldSpace = false;
        if (playerLine) playerLine.useWorldSpace = false;

        // Setup the initial targets
        currentTargetAmplitude = baseTargetAmplitude;
        currentTargetFrequency = baseTargetFrequency;
        currentTargetPhase = baseTargetPhase;

        // Ensure the alignment light materials start off (unless debug is true!)
        if (lightRenderer != null && lightOffMaterial != null && !debugForceSync)
        {
            lightRenderer.material = lightOffMaterial;
        }

        // Initialize the 3 Progress LEDs to the Off material and Point Lights to 0
        for (int i = 0; i < progressLEDs.Length; i++)
        {
            if (progressLEDs[i] != null && lightOffMaterial != null)
            {
                // We use shared material so we don't accidentally leak hundreds of instances over time
                progressLEDs[i].material = lightOffMaterial; 
            }
            if (i < progressPointLights.Length && progressPointLights[i] != null)
            {
                progressPointLights[i].intensity = 0f;
            }
        }

        PickNewTargets();
    }

    void PickNewTargets()
    {
        oldTargetAmplitude = currentTargetAmplitude;
        oldTargetFrequency = currentTargetFrequency;
        oldTargetPhase = currentTargetPhase;

        // Pick random values within our variance offset from the base
        newTargetAmplitude = baseTargetAmplitude + Random.Range(-amplitudeDriftVariance, amplitudeDriftVariance);
        newTargetFrequency = baseTargetFrequency + Random.Range(-frequencyDriftVariance, frequencyDriftVariance);
        newTargetPhase = baseTargetPhase + Random.Range(-phaseDriftVariance, phaseDriftVariance);

        lerpTimer = 0f;
    }

    void Update()
    {
        // 1. Timer Logic
        timer += Time.deltaTime;

        if (timer >= driftInterval)
        {
            PickNewTargets();
            timer = 0f; // Reset the clock
        }

        // 2. Smooth Lerp Logic
        if (lerpTimer < driftLerpDuration)
        {
            lerpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(lerpTimer / driftLerpDuration);
            // Use smoothstep for a softer start and end to the movement
            t = Mathf.SmoothStep(0f, 1f, t);

            currentTargetAmplitude = Mathf.Lerp(oldTargetAmplitude, newTargetAmplitude, t);
            currentTargetFrequency = Mathf.Lerp(oldTargetFrequency, newTargetFrequency, t);
            currentTargetPhase = Mathf.Lerp(oldTargetPhase, newTargetPhase, t);
        }

        // 3. Render both waves using the updated Sine math
        if (targetLine) DrawWave(targetLine, currentTargetAmplitude, currentTargetFrequency, currentTargetPhase);
        if (playerLine) DrawWave(playerLine, playerAmplitude, playerFrequency, playerPhase);

        // 4. Check for Alignment Matches
        CheckSync();
    }

    void CheckSync()
    {
        // Get absolute differences
        float ampDiff = Mathf.Abs(currentTargetAmplitude - playerAmplitude);
        float freqDiff = Mathf.Abs(currentTargetFrequency - playerFrequency);

        // Phase repeats every 2*PI (approx 6.28), so we must find the shortest angular distance
        float pi2 = Mathf.PI * 2f;
        float phaseDiff = Mathf.Abs(currentTargetPhase - playerPhase) % pi2;
        if (phaseDiff > Mathf.PI)
        {
            phaseDiff = pi2 - phaseDiff;
        }

        // Determine if player has matched the target within tolerance
        bool isSynced = (ampDiff <= matchTolerance && freqDiff <= matchTolerance && phaseDiff <= matchTolerance);

        // Debug Override
        if (debugForceSync) isSynced = true;
        
        // -------------------------------------------------------------
        // PROGRESS LOGIC
        // -------------------------------------------------------------
        if (isMinigameComplete) return; // Stop doing math if we already won

        if (isSynced)
        {
            currentProgress += Time.deltaTime;
        }

        // Clamp the progress so it doesn't go below 0 or above Max
        currentProgress = Mathf.Clamp(currentProgress, 0f, timeToComplete);

        if (currentProgress >= timeToComplete)
        {
            isMinigameComplete = true;
            Debug.Log("<color=green>SCIENCE MINIGAME COMPLETED!</color>");
            
            // Spit out the reward
            if (punchcardPrefab != null && punchcardSpawnPoint != null)
            {
                Instantiate(punchcardPrefab, punchcardSpawnPoint.position, punchcardSpawnPoint.rotation);
            }
        }

        // Update the 3 Progress LEDs
        UpdateProgressLEDs();

        // -------------------------------------------------------------
        // MAIN INDICATOR LIGHT LOGIC
        // -------------------------------------------------------------
        // 1. Swap visual materials on the bulb mesh
        if (lightRenderer != null)
        {
            if (isSynced && lightOnMaterial != null)
            {
                lightRenderer.material = lightOnMaterial;
            }
            else if (!isSynced && lightOffMaterial != null)
            {
                lightRenderer.material = lightOffMaterial;
            }
        }
        
        // 2. Toggle the actual unity Point Light
        if (pointLightObject != null)
        {
            pointLightObject.SetActive(isSynced);
        }
    }

    void UpdateProgressLEDs()
    {
        if (progressLEDs == null || progressLEDs.Length == 0) return;

        // Break our total completion time into 3 equal chunks
        float timePerLED = timeToComplete / progressLEDs.Length;

        for (int i = 0; i < progressLEDs.Length; i++)
        {
            // Calculate the start and end threshold times for this specific LED
            float startThreshold = i * timePerLED;
            float endThreshold = (i + 1) * timePerLED;
            
            Light currentLight = (i < progressPointLights.Length) ? progressPointLights[i] : null;

            if (currentProgress >= endThreshold)
            {
                // This LED is fully locked IN
                if (progressLEDs[i] != null) progressLEDs[i].material = lightOnMaterial;
                if (currentLight != null) currentLight.intensity = maxProgressLightIntensity;
            }
            else if (currentProgress > startThreshold && currentProgress < endThreshold)
            {
                // This LED is currently "filling up". 
                float fractionFull = (currentProgress - startThreshold) / timePerLED;
                
                // Realistic erratic flicker using Perlin noise
                float noise = Mathf.PerlinNoise(Time.time * 25f, i * 10f);
                
                // Add a baseline pulse/blink that gets faster as it fills up
                float blinkRate = Mathf.Lerp(5f, 25f, fractionFull);
                float sineBlink = Mathf.Sin(Time.time * blinkRate);
                
                // Combine sine wave and noise for an erratic, struggling-to-turn-on mechanical flicker
                bool isOn = (sineBlink + noise) > 0.8f;

                if (progressLEDs[i] != null) 
                {
                    progressLEDs[i].material = isOn ? lightOnMaterial : lightOffMaterial;
                }
                
                if (currentLight != null) 
                {
                    // Scale baseline intensity by progress fraction so it physically grows brighter, 
                    // and apply flickering bursts
                    float flickerMultiplier = isOn ? 1.2f : 0.4f;
                    currentLight.intensity = maxProgressLightIntensity * fractionFull * flickerMultiplier;
                }
            }
            else 
            {
                // This LED has zero progress
                if (progressLEDs[i] != null) progressLEDs[i].material = lightOffMaterial;
                if (currentLight != null) currentLight.intensity = 0f;
            }
        }
    }

    void DrawWave(LineRenderer lr, float amp, float freq, float phase)
    {
        // The horizontal 'crawl' mimics the electron beam sweeping the CRT
        float timeOffset = Time.time * runSpeed;

        for (int i = 0; i < numPoints; i++)
        {
            // Calculate a normalized X position from 0 to 1
            float progress = (float)i / (numPoints - 1);
            
            // Map the progress across our desired wave width, centered at 0
            float xPos = (progress * waveWidth) - (waveWidth / 2f);

            // THE SINE MATH: y = A * sin(B * (x + offset) + phase)
            // We multiply the xPos by visualDensity to compress the wave visually without changing the math speed
            float yPos = amp * Mathf.Sin((freq * xPos * visualDensity) + phase + timeOffset);

            // Since useWorldSpace is false, we set the positions locally. 
            // We push Z slightly back for the player so Z-fighting doesn't occur.
            float zOffset = (lr == playerLine) ? -0.01f : 0f;

            lr.SetPosition(i, new Vector3(xPos, yPos, zOffset));
        }
    }
}
