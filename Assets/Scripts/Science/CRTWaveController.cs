using UnityEngine;

public class CRTWaveController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("This is now assigned dynamically by the cassette tape!")]
    [HideInInspector] public RadioBeacon linkedBeacon;

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
    [Range(0.16f, 1.1f)] public float playerAmplitude = 0.5f;
    [Range(6.2f, 10f)] public float playerFrequency = 8.0f;
    [Range(0f, 12.56f)] public float playerPhase = 2.5f;

    [Header("Target Wave Settings (Amber)")]
    public float baseTargetAmplitude = 0.5f;
    public float baseTargetFrequency = 8.0f;
    public float baseTargetPhase = 2.5f;

    [Header("Target Drift Settings (Unbound)")]
    [Tooltip("The minimum time the wave holds steady before mutating.")]
    public float minDriftInterval = 2.0f;
    [Tooltip("The maximum time the wave holds steady before mutating.")]
    public float maxDriftInterval = 6.0f;
    [Tooltip("How long it takes to lerp to the new mutation.")]
    public float driftLerpDuration = 1.5f;

    [Tooltip("How much the Amplitude can randomly increase/decrease in a single jump.")]
    public float amplitudeDriftVariance = 0.2f;
    [Tooltip("How much the Frequency can randomly increase/decrease in a single jump.")]
    public float frequencyDriftVariance = 1.0f;
    [Tooltip("How much the Phase can randomly increase/decrease in a single jump.")]
    public float phaseDriftVariance = 2.0f;

    private float currentDriftInterval;

    // Internal variables
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

    [HideInInspector] public float currentProgress = 0f;
    [HideInInspector] public bool isMinigameComplete = false;

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
            case 0: // Mutate Amplitude
                float rawNewAmp = currentTargetAmplitude + Random.Range(-amplitudeDriftVariance, amplitudeDriftVariance);
                newTargetAmplitude = Mathf.Clamp(rawNewAmp, 0.16f, 1.1f);
                break;

            case 1: // Mutate Frequency
                float rawNewFreq = currentTargetFrequency + Random.Range(-frequencyDriftVariance, frequencyDriftVariance);
                newTargetFrequency = Mathf.Clamp(rawNewFreq, 6.2f, 10.0f);
                break;

            case 2: // Mutate Phase
                float rawNewPhase = currentTargetPhase + Random.Range(-phaseDriftVariance, phaseDriftVariance);
                newTargetPhase = Mathf.Clamp(rawNewPhase, 0f, 12.56f); // Locked to Golden Number
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
        currentProgress = Mathf.Clamp(currentProgress, 0f, timeToComplete);

        if (currentProgress >= timeToComplete)
        {
            isMinigameComplete = true;
            Debug.Log("<color=green>SCIENCE MINIGAME COMPLETED!</color>");

            if (punchcardPrefab != null && punchcardSpawnPoint != null)
            {
                // Spawn the card as a child of the spawn point
                GameObject spawnedCard = Instantiate(punchcardPrefab, punchcardSpawnPoint);

                // 1. Snap to the exact local coordinates of the spawn point
                spawnedCard.transform.localPosition = Vector3.zero;
                spawnedCard.transform.localRotation = Quaternion.identity;

                // 2. THE BULLETPROOF SCALE FIX
                // Cancel out any scaled-up parent objects by dividing by the lossyScale
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

        float timePerLED = timeToComplete / progressLEDs.Length;

        for (int i = 0; i < progressLEDs.Length; i++)
        {
            float startThreshold = i * timePerLED;
            float endThreshold = (i + 1) * timePerLED;
            Light currentLight = (i < progressPointLights.Length) ? progressPointLights[i] : null;

            if (currentProgress >= endThreshold)
            {
                if (progressLEDs[i] != null) progressLEDs[i].material = lightOnMaterial;
                if (currentLight != null) currentLight.intensity = maxProgressLightIntensity;
            }
            else if (currentProgress > startThreshold && currentProgress < endThreshold)
            {
                float fractionFull = (currentProgress - startThreshold) / timePerLED;
                float noise = Mathf.PerlinNoise(Time.time * 25f, i * 10f);
                float blinkRate = Mathf.Lerp(5f, 25f, fractionFull);
                float sineBlink = Mathf.Sin(Time.time * blinkRate);

                bool isOn = (sineBlink + noise) > 0.8f;

                if (progressLEDs[i] != null) progressLEDs[i].material = isOn ? lightOnMaterial : lightOffMaterial;
                if (currentLight != null) currentLight.intensity = maxProgressLightIntensity * fractionFull * (isOn ? 1.2f : 0.4f);
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
        linkedBeacon = sourceBeacon;

        Debug.Log($"<color=cyan>CRT WAVE CONTROLLER ONLINE. Processing data for: {(linkedBeacon != null ? linkedBeacon.name : "Unknown POI")}</color>");

        currentProgress = 0f;
        isMinigameComplete = false;

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