using UnityEngine;

public class CRTWaveController : MonoBehaviour
{
    [Header("Dependencies")]
    [HideInInspector] public RadioBeacon linkedBeacon;

    [Header("Audio System (CRT)")]
    public AudioSource crtLoopSource;
    public AudioSource crtOneShotSource;
    public AudioClip crtTurnOnSound;
    public AudioClip crtLoopSound;
    public AudioClip crtTurnOffSound;

    [Header("Audio System (Minigame Events)")]
    public AudioClip ledCompleteSound;
    [Range(0f, 1f)] public float ledCompleteVolume = 1.0f;
    [Tooltip("Minimum pitch for the LED complete sound to prevent repetitive audio.")]
    [Range(0.5f, 1.5f)] public float ledMinPitch = 0.9f; // <--- NEW: Pitch Variance Min
    [Tooltip("Maximum pitch for the LED complete sound.")]
    [Range(0.5f, 1.5f)] public float ledMaxPitch = 1.15f; // <--- NEW: Pitch Variance Max

    public AudioClip punchcardSpawnSound;
    [Range(0f, 1f)] public float punchcardSpawnVolume = 1.0f;

    [Tooltip("Control how loud the constant CRT hum is.")]
    [Range(0f, 1f)] public float crtLoopVolume = 0.5f;

    [Header("Line Renderers")]
    public LineRenderer targetLine;
    public LineRenderer playerLine;

    [Header("Minigame Logic/Feedback")]
    public bool debugForceSync = false;
    public MeshRenderer lightRenderer;
    public GameObject pointLightObject;

    [Header("Progress LEDs")]
    public MeshRenderer[] progressLEDs = new MeshRenderer[3];
    public Light[] progressPointLights = new Light[3];
    public float maxProgressLightIntensity = 1.0f;

    [Header("Materials")]
    public Material lightOffMaterial;
    public Material lightOnMaterial;

    [Header("Time & Penalty Settings")]
    public float interruptionPenaltyPercent = 0.65f;
    private float activeTimeToComplete = 6.0f;

    [Header("Rewards")]
    public GameObject punchcardPrefab;
    public Transform punchcardSpawnPoint;

    [Header("Wave Rendering Settings")]
    public int numPoints = 200;
    public float waveWidth = 0.65f;
    public float runSpeed = -2f;
    public float visualDensity = 5f;
    public float matchTolerance = 0.2f;

    [Header("Player Knobs (Inputs)")]
    [Range(0.16f, 1.1f)] public float playerAmplitude = 0.5f;
    [Range(6.2f, 10f)] public float playerFrequency = 8.0f;
    [Range(0f, 12.56f)] public float playerPhase = 2.5f;

    [Header("Live Debug (Watch in Play Mode)")]
    public float currentProgress = 0f;
    public float completionTimeExtension = 0f;
    public bool isMinigameComplete = false;
    public bool isSignalBlockedByMonster = false; // <--- YOUR NEW BOOL

    // Internal variables
    private DifficultyProfile currentProfile;
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

    private int completedLEDCount = 0;

    void Start()
    {
        if (targetLine) targetLine.positionCount = numPoints;
        if (playerLine) playerLine.positionCount = numPoints;
        if (targetLine) targetLine.useWorldSpace = false;
        if (playerLine) playerLine.useWorldSpace = false;

        if (lightRenderer != null && lightOffMaterial != null && !debugForceSync)
        {
            lightRenderer.material = lightOffMaterial;
        }

        for (int i = 0; i < progressLEDs.Length; i++)
        {
            if (progressLEDs[i] != null && lightOffMaterial != null) progressLEDs[i].material = lightOffMaterial;
            if (i < progressPointLights.Length && progressPointLights[i] != null) progressPointLights[i].intensity = 0f;
        }
    }

    void Update()
    {
        if (currentProfile == null) return;

        timer += Time.deltaTime;

        if (timer >= currentDriftInterval)
        {
            PickNewTargets();
            timer = 0f;
            currentDriftInterval = Random.Range(currentProfile.minDriftInterval, currentProfile.maxDriftInterval);
        }

        if (lerpTimer < currentProfile.driftLerpDuration)
        {
            lerpTimer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(lerpTimer / currentProfile.driftLerpDuration));

            currentTargetAmplitude = Mathf.Lerp(oldTargetAmplitude, newTargetAmplitude, t);
            currentTargetFrequency = Mathf.Lerp(oldTargetFrequency, newTargetFrequency, t);
            currentTargetPhase = Mathf.Lerp(oldTargetPhase, newTargetPhase, t);
        }

        if (targetLine) DrawWave(targetLine, currentTargetAmplitude, currentTargetFrequency, currentTargetPhase);
        if (playerLine) DrawWave(playerLine, playerAmplitude, playerFrequency, playerPhase);

        CheckSync();
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

    public void ApplyInterruptionPenalty()
    {
        if (isMinigameComplete || currentProgress <= 0) return;
        float penaltyAmount = currentProgress * interruptionPenaltyPercent;
        completionTimeExtension += penaltyAmount;
        Debug.Log($"<color=orange>SCIENCE: Progress interrupted! {penaltyAmount:F1}s added to completion debt.</color>");
    }

    void PickNewTargets()
    {
        if (currentProfile == null) return;

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
                float rawNewAmp = currentTargetAmplitude + Random.Range(-currentProfile.amplitudeDriftVariance, currentProfile.amplitudeDriftVariance);
                newTargetAmplitude = Mathf.Clamp(rawNewAmp, 0.16f, 1.1f);
                break;
            case 1:
                float rawNewFreq = currentTargetFrequency + Random.Range(-currentProfile.frequencyDriftVariance, currentProfile.frequencyDriftVariance);
                newTargetFrequency = Mathf.Clamp(rawNewFreq, 6.2f, 10.0f);
                break;
            case 2:
                float rawNewPhase = currentTargetPhase + Random.Range(-currentProfile.phaseDriftVariance, currentProfile.phaseDriftVariance);
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

        if (isSynced && !isSignalBlockedByMonster)
        {
            currentProgress += Time.deltaTime;
        }
        float totalTargetTime = activeTimeToComplete + completionTimeExtension;
        currentProgress = Mathf.Clamp(currentProgress, 0f, totalTargetTime);

        if (currentProgress >= totalTargetTime)
        {
            isMinigameComplete = true;
            Debug.Log("<color=green>SCIENCE MINIGAME COMPLETED!</color>");

            // --- THE FIX: Reset pitch to exactly 1.0 before playing the punchcard sound ---
            if (crtOneShotSource != null) crtOneShotSource.pitch = 1f;

            if (crtOneShotSource != null && punchcardSpawnSound != null)
            {
                crtOneShotSource.PlayOneShot(punchcardSpawnSound, punchcardSpawnVolume);
            }

            if (punchcardPrefab != null && punchcardSpawnPoint != null)
            {
                // 1. Spawn the card as a child of the spawn point
                GameObject spawnedCard = Instantiate(punchcardPrefab, punchcardSpawnPoint);

                // 2. Snap it exactly to the spawn point's center and rotation
                spawnedCard.transform.localPosition = Vector3.zero;
                spawnedCard.transform.localRotation = Quaternion.identity;

                // 3. Force it to use the exact scale you saved on the prefab!
                spawnedCard.transform.localScale = punchcardPrefab.transform.localScale;

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

        float timePerLED = activeTimeToComplete / progressLEDs.Length;
        int ledsDoneThisFrame = 0;

        for (int i = 0; i < progressLEDs.Length; i++)
        {
            float startThreshold = i * timePerLED;
            float endThreshold = (i + 1) * timePerLED;

            if (i == progressLEDs.Length - 1) endThreshold = activeTimeToComplete + completionTimeExtension;

            Light currentLight = (i < progressPointLights.Length) ? progressPointLights[i] : null;

            if (currentProgress >= endThreshold)
            {
                if (progressLEDs[i] != null) progressLEDs[i].material = lightOnMaterial;
                if (currentLight != null) currentLight.intensity = maxProgressLightIntensity;
                ledsDoneThisFrame++;
            }
            else if (currentProgress > startThreshold && currentProgress < endThreshold)
            {
                float fractionFull = (currentProgress - startThreshold) / (endThreshold - startThreshold);
                float blinkRate = Mathf.Lerp(3f, 12f, fractionFull);
                float sineValue = Mathf.Sin(Time.time * blinkRate);
                bool isOn = sineValue > 0f;

                if (progressLEDs[i] != null) progressLEDs[i].material = isOn ? lightOnMaterial : lightOffMaterial;
                if (currentLight != null) currentLight.intensity = maxProgressLightIntensity * fractionFull * Mathf.Lerp(0.2f, 1f, (sineValue + 1f) / 2f);
            }
            else
            {
                if (progressLEDs[i] != null) progressLEDs[i].material = lightOffMaterial;
                if (currentLight != null) currentLight.intensity = 0f;
            }
        }

        // --- NEW: Randomize the pitch right before the LED sound plays ---
        if (ledsDoneThisFrame > completedLEDCount)
        {
            if (crtOneShotSource != null && ledCompleteSound != null)
            {
                crtOneShotSource.pitch = Random.Range(ledMinPitch, ledMaxPitch);
                crtOneShotSource.PlayOneShot(ledCompleteSound, ledCompleteVolume);
            }
            completedLEDCount = ledsDoneThisFrame;
        }
    }

    public void TurnOffMachine(bool playSound = true)
    {
        bool wasOn = this.enabled;
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

        if (wasOn && playSound)
        {
            if (crtLoopSource != null) crtLoopSource.Stop();

            // --- THE FIX: Reset pitch to exactly 1.0 before playing the turn-off sound ---
            if (crtOneShotSource != null) crtOneShotSource.pitch = 1f;

            if (crtOneShotSource != null && crtTurnOffSound != null) crtOneShotSource.PlayOneShot(crtTurnOffSound);
        }
    }

    public void TurnOnMachine(RadioBeacon sourceBeacon, DifficultyProfile profile)
    {
        bool wasOff = !this.enabled;
        currentProfile = profile;

        if (currentProfile != null)
        {
            activeTimeToComplete = Random.Range(currentProfile.minigameCompletionTime.x, currentProfile.minigameCompletionTime.y);
            Debug.Log($"<color=yellow>Minigame active. Required sync time rolled: {activeTimeToComplete:F1} seconds.</color>");
        }
        else
        {
            activeTimeToComplete = 6.0f; // Fallback just in case
        }

        // When a new tape is inserted...
        if (linkedBeacon != sourceBeacon)
        {
            currentProgress = 0f;
            completionTimeExtension = 0f;
            isMinigameComplete = false;
            completedLEDCount = 0;
        }
        currentTargetAmplitude = Random.Range(0.16f, 1.1f);
        currentTargetFrequency = Random.Range(6.2f, 10.0f);
        currentTargetPhase = Random.Range(0f, 12.56f);

        linkedBeacon = sourceBeacon;
        Debug.Log($"<color=cyan>CRT WAVE CONTROLLER ONLINE. Processing data for: {(linkedBeacon != null ? linkedBeacon.name : "Unknown POI")}</color>");

        if (currentProfile != null)
        {
            currentDriftInterval = Random.Range(currentProfile.minDriftInterval, currentProfile.maxDriftInterval);
        }

        PickNewTargets();

        if (targetLine) targetLine.gameObject.SetActive(true);
        if (playerLine) playerLine.gameObject.SetActive(true);
        this.enabled = true;

        if (wasOff)
        {
            if (crtOneShotSource != null) crtOneShotSource.pitch = 1f;
            if (crtOneShotSource != null && crtTurnOnSound != null) crtOneShotSource.PlayOneShot(crtTurnOnSound);

            if (crtLoopSource != null && crtLoopSound != null)
            {
                crtLoopSource.clip = crtLoopSound;
                crtLoopSource.loop = true;
                crtLoopSource.volume = crtLoopVolume;

                if (!crtLoopSource.isPlaying) crtLoopSource.Play();
            }
        }
    }
    public void NotifyPunchcardCollected()
    {
        TurnOffMachine(true);
        if (linkedBeacon != null)
        {
            linkedBeacon.isCompleted = true;
            Debug.Log("<color=magenta>Beacon notified: Ready to fade signal upon departure.</color>");
        }
    }
}