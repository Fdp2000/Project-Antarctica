using UnityEngine;

public class CRTWaveController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("This is now assigned dynamically by the cassette tape!")]
    [HideInInspector] public RadioBeacon linkedBeacon; [Header("Line Renderers")]
    [Tooltip("The Amber target wave that the player tries to match.")]
    public LineRenderer targetLine;
    [Tooltip("The Green player wave controlled by the physical knobs.")]
    public LineRenderer playerLine;

    [Header("Minigame Logic/Feedback")]
    [Tooltip("Check this to force the bulb ON for testing purposes.")]
    public bool debugForceSync = false; [Tooltip("The MeshRenderer of the physical light bulb model.")]
    public MeshRenderer lightRenderer; [Tooltip("An actual Unity Light component GameObject to turn on/off.")]
    public GameObject pointLightObject;

    [Header("Progress LEDs")]
    [Tooltip("The 3 small progress LEDs. Order them Bottom (Index 0) to Top (Index 2).")]
    public MeshRenderer[] progressLEDs = new MeshRenderer[3]; [Tooltip("The 3 Unity Point Lights accompanying the LEDs. Order them matching the renderers.")]
    public Light[] progressPointLights = new Light[3];
    [Tooltip("The maximum intensity the point light should reach when its chunk of progress is complete.")]
    public float maxProgressLightIntensity = 1.0f;

    [Header("Materials")]
    [Tooltip("The material to use when the waves are OUT of sync (Off).")]
    public Material lightOffMaterial;
    [Tooltip("The material to use when the waves are IN sync (Emissive/On).")]
    public Material lightOnMaterial;

    [Header("Time & Penalty Settings")]
    [Tooltip("How long (in continuous seconds) the player must remain in sync to fully light all 3 LEDs and win.")]
    public float timeToComplete = 6.0f; [Tooltip("How much of current progress is lost (added to total time) if interrupted (e.g., 0.65 = 65%).")]
    public float interruptionPenaltyPercent = 0.65f;

    [Header("Rewards")]
    [Tooltip("The GameObject prefab to spawn when the player wins (the punchcard).")]
    public GameObject punchcardPrefab;
    [Tooltip("Where the punchcard should physically appear on the machine.")]
    public Transform punchcardSpawnPoint;

    [Header("Wave Rendering Settings")]
    [Tooltip("Number of points on the line. Higher = smoother curve.")]
    public int numPoints = 200; [Tooltip("Total width of the wave rendered across the screen.")]
    public float waveWidth = 0.65f; [Tooltip("How fast the wave 'crawls' horizontally across the screen.")]
    public float runSpeed = -2f; [Tooltip("Visual multiplier for frequency to ensure multiple peaks fit on screen.")]
    public float visualDensity = 5f;
    [Tooltip("How close the player's knobs must be to the drifting target math.")]
    public float matchTolerance = 0.2f;

    [Header("Player Knobs (Inputs)")]
    [Range(0.16f, 1.1f)] public float playerAmplitude = 0.5f;
    [Range(6.2f, 10f)] public float playerFrequency = 8.0f;
    [Range(0f, 12.56f)] public float playerPhase = 2.5f;

    [Header("Target Wave Settings (Amber)")]
    public float baseTargetAmplitude = 0.5f;
    public float baseTargetFrequency = 8.0f;
    public float baseTargetPhase = 2.5f; [Header("Target Drift Settings (Unbound)")]
    [Tooltip("The minimum time the wave holds steady before mutating.")]
    public float minDriftInterval = 2.0f;
    [Tooltip("The maximum time the wave holds steady before mutating.")]
    public float maxDriftInterval = 6.0f; [Tooltip("How long it takes to lerp to the new mutation.")]
    public float driftLerpDuration = 1.5f;

    public float amplitudeDriftVariance = 0.2f;
    public float frequencyDriftVariance = 1.0f;
    public float phaseDriftVariance = 2.0f;

    [Header("Live Debug (Watch in Play Mode)")]
    [Tooltip("The current accumulated progress in seconds.")]
    public float currentProgress = 0f; [Tooltip("The hidden 'Time Debt'. Final LED finishes when progress hits (timeToComplete + completionTimeExtension).")]
    public float completionTimeExtension = 0f;
    [Tooltip("Has the minigame completely finished?")]
    public bool isMinigameComplete = false;

    // Internal variables
    private float currentDriftInterval;
    private float currentTargetAmplitude;
    private float currentTargetFrequency;
    private float currentTargetPhase;

    private float oldTargetAmplitude;
    private float oldTargetFrequency;
    private float oldTargetPhase;

    private float newTargetAmplitude;
    private float newTargetFrequency;
    private float newTargetPhase;

    private float timer = 0f;
    private float lerpTimer = 0f;

    void Start()
    {
        if (targetLine) targetLine.positionCount = numPoints;
        if (playerLine) playerLine.positionCount = numPoints;
        if (targetLine) targetLine.useWorldSpace = false;
        if (playerLine) playerLine.useWorldSpace = false;

        currentTargetAmplitude = baseTargetAmplitude;
        currentTargetFrequency = baseTargetFrequency;
        currentTargetPhase = baseTargetPhase;

        if (lightRenderer != null && lightOffMaterial != null && !debugForceSync)
        {
            lightRenderer.material = lightOffMaterial;
        }

        for (int i = 0; i < progressLEDs.Length; i++)
        {
            if (progressLEDs[i] != null && lightOffMaterial != null) progressLEDs[i].material = lightOffMaterial;
            if (i < progressPointLights.Length && progressPointLights[i] != null) progressPointLights[i].intensity = 0f;
        }

        currentDriftInterval = Random.Range(minDriftInterval, maxDriftInterval);
        PickNewTargets();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= currentDriftInterval)
        {
            PickNewTargets();
            timer = 0f;
            currentDriftInterval = Random.Range(minDriftInterval, maxDriftInterval);
        }

        if (lerpTimer < driftLerpDuration)
        {
            lerpTimer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(lerpTimer / driftLerpDuration));

            currentTargetAmplitude = Mathf.Lerp(oldTargetAmplitude, newTargetAmplitude, t);
            currentTargetFrequency = Mathf.Lerp(oldTargetFrequency, newTargetFrequency, t);
            currentTargetPhase = Mathf.Lerp(oldTargetPhase, newTargetPhase, t);
        }

        if (targetLine) DrawWave(targetLine, currentTargetAmplitude, currentTargetFrequency, currentTargetPhase);
        if (playerLine) DrawWave(playerLine, playerAmplitude, playerFrequency, playerPhase);

        CheckSync();
    }

    // --- PENALTY METHOD ---
    public void ApplyInterruptionPenalty()
    {
        if (isMinigameComplete || currentProgress <= 0) return;

        // Calculate 65% of the progress they achieved so far
        float penaltyAmount = currentProgress * interruptionPenaltyPercent;

        // Add it to the time debt
        completionTimeExtension += penaltyAmount;

        Debug.Log($"<color=orange>SCIENCE: Progress interrupted! {penaltyAmount:F1}s added to completion debt.</color>");
    }

    void PickNewTargets()
    {
        oldTargetAmplitude = currentTargetAmplitude;
        oldTargetFrequency = currentTargetFrequency;
        oldTargetPhase = currentTargetPhase;

        newTargetAmplitude = currentTargetAmplitude;
        newTargetFrequency = currentTargetFrequency;
        newTargetPhase = currentTargetPhase;

        int propertyToMutate = Random.Range(0, 3);

        switch (propertyToMutate)
        {
            case 0:
                float rawNewAmp = currentTargetAmplitude + Random.Range(-amplitudeDriftVariance, amplitudeDriftVariance);
                newTargetAmplitude = Mathf.Clamp(rawNewAmp, 0.16f, 1.1f);
                break;
            case 1:
                float rawNewFreq = currentTargetFrequency + Random.Range(-frequencyDriftVariance, frequencyDriftVariance);
                newTargetFrequency = Mathf.Clamp(rawNewFreq, 6.2f, 10.0f);
                break;
            case 2:
                float rawNewPhase = currentTargetPhase + Random.Range(-phaseDriftVariance, phaseDriftVariance);
                newTargetPhase = Mathf.Clamp(rawNewPhase, 0f, 12.56f);
                break;
        }

        lerpTimer = 0f;
    }

    void CheckSync()
    {
        float ampDiff = Mathf.Abs(currentTargetAmplitude - playerAmplitude);
        float freqDiff = Mathf.Abs(currentTargetFrequency - playerFrequency);

        float pi2 = Mathf.PI * 2f;
        float phaseDiff = Mathf.Abs(currentTargetPhase - playerPhase) % pi2;
        if (phaseDiff > Mathf.PI) phaseDiff = pi2 - phaseDiff;

        bool isSynced = (ampDiff <= matchTolerance && freqDiff <= matchTolerance && phaseDiff <= matchTolerance);
        if (debugForceSync) isSynced = true;

        if (isMinigameComplete) return;

        if (isSynced) currentProgress += Time.deltaTime;

        // The target required time is the standard 6 seconds + any penalty debt accrued
        float totalTargetTime = timeToComplete + completionTimeExtension;
        currentProgress = Mathf.Clamp(currentProgress, 0f, totalTargetTime);

        if (currentProgress >= totalTargetTime)
        {
            isMinigameComplete = true;
            Debug.Log("<color=green>SCIENCE MINIGAME COMPLETED!</color>");

            if (punchcardPrefab != null && punchcardSpawnPoint != null)
            {
                GameObject spawnedCard = Instantiate(punchcardPrefab, punchcardSpawnPoint);
                spawnedCard.transform.localPosition = Vector3.zero;
                spawnedCard.transform.localRotation = Quaternion.identity;

                Vector3 parentScale = punchcardSpawnPoint.lossyScale;
                Vector3 prefabScale = punchcardPrefab.transform.localScale;

                spawnedCard.transform.localScale = new Vector3(
                    prefabScale.x / parentScale.x,
                    prefabScale.y / parentScale.y,
                    prefabScale.z / parentScale.z
                );

                PunchcardInteractable interactable = spawnedCard.GetComponent<PunchcardInteractable>();
                if (interactable != null) interactable.waveController = this;
            }
        }

        UpdateProgressLEDs();

        if (lightRenderer != null)
        {
            lightRenderer.material = isSynced ? (lightOnMaterial != null ? lightOnMaterial : lightRenderer.material) : (lightOffMaterial != null ? lightOffMaterial : lightRenderer.material);
        }

        if (pointLightObject != null) pointLightObject.SetActive(isSynced);
    }

    void UpdateProgressLEDs()
    {
        if (progressLEDs == null || progressLEDs.Length == 0) return;

        // Visual mapping stays completely tied to the original 6 seconds so it doesn't drop backwards
        float timePerLED = timeToComplete / progressLEDs.Length;

        for (int i = 0; i < progressLEDs.Length; i++)
        {
            float startThreshold = i * timePerLED;
            float endThreshold = (i + 1) * timePerLED;

            // ONLY the final LED absorbs the penalty debt, extending its threshold
            if (i == progressLEDs.Length - 1)
            {
                endThreshold = timeToComplete + completionTimeExtension;
            }

            Light currentLight = (i < progressPointLights.Length) ? progressPointLights[i] : null;

            if (currentProgress >= endThreshold)
            {
                if (progressLEDs[i] != null) progressLEDs[i].material = lightOnMaterial;
                if (currentLight != null) currentLight.intensity = maxProgressLightIntensity;
            }
            else if (currentProgress > startThreshold && currentProgress < endThreshold)
            {
                float fractionFull = (currentProgress - startThreshold) / (endThreshold - startThreshold);

                // Smooth, non-violent pulsing logic
                float blinkRate = Mathf.Lerp(3f, 12f, fractionFull);
                float sineValue = Mathf.Sin(Time.time * blinkRate);

                bool isOn = sineValue > 0f;

                if (progressLEDs[i] != null) progressLEDs[i].material = isOn ? lightOnMaterial : lightOffMaterial;

                if (currentLight != null)
                {
                    float lightPulse = Mathf.Lerp(0.2f, 1f, (sineValue + 1f) / 2f);
                    currentLight.intensity = maxProgressLightIntensity * fractionFull * lightPulse;
                }
            }
            else
            {
                if (progressLEDs[i] != null) progressLEDs[i].material = lightOffMaterial;
                if (currentLight != null) currentLight.intensity = 0f;
            }
        }
    }

    void DrawWave(LineRenderer lr, float amp, float freq, float phase)
    {
        float timeOffset = Time.time * runSpeed;
        for (int i = 0; i < numPoints; i++)
        {
            float progress = (float)i / (numPoints - 1);
            float xPos = (progress * waveWidth) - (waveWidth / 2f);
            float yPos = amp * Mathf.Sin((freq * xPos * visualDensity) + phase + timeOffset);
            float zOffset = (lr == playerLine) ? -0.01f : 0f;
            lr.SetPosition(i, new Vector3(xPos, yPos, zOffset));
        }
    }

    public void TurnOffMachine()
    {
        this.enabled = false;
        if (targetLine) targetLine.gameObject.SetActive(false);
        if (playerLine) playerLine.gameObject.SetActive(false);
        if (lightRenderer != null && lightOffMaterial != null) lightRenderer.material = lightOffMaterial;
        if (pointLightObject != null) pointLightObject.SetActive(false);

        for (int i = 0; i < progressLEDs.Length; i++)
        {
            if (progressLEDs[i] != null && lightOffMaterial != null) progressLEDs[i].material = lightOffMaterial;
            if (i < progressPointLights.Length && progressPointLights[i] != null) progressPointLights[i].intensity = 0f;
        }
    }

    public void TurnOnMachine(RadioBeacon sourceBeacon)
    {
        // If it is a completely new tape, reset everything. 
        // If it's the SAME tape, keep the progress and the time debt!
        if (linkedBeacon != sourceBeacon)
        {
            currentProgress = 0f;
            completionTimeExtension = 0f;
            isMinigameComplete = false;
        }

        linkedBeacon = sourceBeacon;

        Debug.Log($"<color=cyan>CRT WAVE CONTROLLER ONLINE. Processing data for: {(linkedBeacon != null ? linkedBeacon.name : "Unknown POI")}</color>");

        PickNewTargets();

        if (targetLine) targetLine.gameObject.SetActive(true);
        if (playerLine) playerLine.gameObject.SetActive(true);
        this.enabled = true;
    }

    public void NotifyPunchcardCollected()
    {
        TurnOffMachine();

        if (linkedBeacon != null)
        {
            linkedBeacon.isCompleted = true;
            Debug.Log("<color=magenta>Beacon notified: Ready to fade signal upon departure.</color>");
        }
    }
}