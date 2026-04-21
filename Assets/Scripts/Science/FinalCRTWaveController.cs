using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class FinalCRTWaveController : MonoBehaviour
{
    [Header("=== THE INTRUDER (Ending Sequence) ===")]
    public AudioSource intruderAudioSource;
    public AudioClip intruderLoopClip;
    [Range(0f, 1f)] public float intruderVolume = 1.0f;

    [Tooltip("The player's main camera (so we can violently snap it).")]
    public Transform playerCamera;

    [Tooltip("The player's movement/look script (so we can disable it).")]
    public MonoBehaviour fpsController; // <--- ADD THIS LINE

    [Tooltip("How fast the camera whips around to face the end node (in seconds).")]
    public float cameraSnapDuration = 0.15f;

    [Tooltip("Where the sound starts (e.g., the shed door).")]
    public Transform intruderStartNode;

    [Tooltip("Where the sound ends (e.g., right behind the player).")]
    public Transform intruderEndNode;

    [Tooltip("How many seconds it takes for the sound to travel from Start to End.")]
    public float intruderTravelDuration = 8.0f;

    [Tooltip("Seconds of pure silence AFTER the lights die, but BEFORE the intruder sound starts.")]
    public float silenceBeforeIntruder = 3.0f;

    [Tooltip("Seconds of pure silence AFTER the intruder sound stops, but BEFORE the credits roll.")]
    public float silenceBeforeCredits = 2.0f;

    [Header("Audio System (CRT)")]
    public AudioSource crtLoopSource;
    public AudioSource crtOneShotSource;
    public AudioClip crtTurnOnSound;
    public AudioClip crtLoopSound;
    public AudioClip crtTurnOffSound;

    [Header("Audio System (Minigame Events)")]
    public AudioClip ledCompleteSound;
    [Range(0f, 1f)] public float ledCompleteVolume = 1.0f;
    [Range(0.5f, 1.5f)] public float ledMinPitch = 0.9f;
    [Range(0.5f, 1.5f)] public float ledMaxPitch = 1.15f;

    [Range(0f, 1f)] public float crtLoopVolume = 0.5f;

    [Header("Line Renderers")]
    public LineRenderer targetLine;
    public LineRenderer playerLine;

    [Header("Minigame Logic/Feedback")]
    public bool debugForceSync = false;
    [Tooltip("Hold this key to auto-sync the wave without using the inspector.")]
    public KeyCode debugForceSyncKey = KeyCode.G; // The '~' key above Tab
    public MeshRenderer lightRenderer;
    public GameObject pointLightObject;

    [Header("Progress LEDs")]
    public MeshRenderer[] progressLEDs = new MeshRenderer[3];
    public Light[] progressPointLights = new Light[3];
    public float maxProgressLightIntensity = 1.0f;
    public Material lightOffMaterial;
    public Material lightOnMaterial;

    [Header("=== FINAL PUZZLE DIFFICULTY ===")]
    public float timeToComplete = 10.0f;
    public float baseTargetAmplitude = 0.7f;
    public float baseTargetFrequency = 8.0f;
    public float baseTargetPhase = 10.43f;
    public float minDriftInterval = 4.0f;
    public float maxDriftInterval = 6.0f;
    public float driftLerpDuration = 3.0f;
    public float amplitudeDriftVariance = 1.3f;
    public float frequencyDriftVariance = 1.5f;
    public float phaseDriftVariance = 1.5f;

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

    [Header("=== ENDGAME CINEMATIC ===")]
    public GameObject dataTransmittedText;
    public AudioSource externalScreechSource;
    public AudioClip monsterScreechClip;
    public Light[] shedSpotlights;

    // --- NEW: The physical bulb meshes ---
    [Tooltip("The MeshRenderers for the physical light bulbs so their emission can be toggled.")]
    public MeshRenderer[] shedBulbRenderers;

    public AudioSource lightFlickerSource;
    public AudioClip fastFlickerClip;
    public AudioClip finalPowerDownClip;

    // ==========================================
    // --- EXPOSED CINEMATIC TIMINGS ---
    // ==========================================
    [Header("Cinematic Timings")]
    [Tooltip("How fast the text flashes on and off.")]
    public float textBlinkInterval = 0.4f;
    [Tooltip("How long the text stays solid before the machine turns off.")]
    public float textSolidDuration = 2.0f;
    [Tooltip("Silence between the machine turning off and the monster screech.")]
    public float delayBeforeScreech = 1.0f;
    [Tooltip("How long to wait for the screech audio to finish before lights flicker.")]
    public float delayAfterScreech = 1.5f;

    public int LightFlickerAmount = 5;
    [Tooltip("X = Min, Y = Max seconds the lights stay OFF during a flicker.")]
    public Vector2 flickerOffDuration = new Vector2(0.05f, 0.2f);
    [Tooltip("X = Min, Y = Max seconds the lights stay ON during a flicker.")]
    public Vector2 flickerOnDuration = new Vector2(0.1f, 0.4f);

    [Tooltip("X = Min, Y = Max seconds of pure darkness before the credits roll.")]
    public Vector2 delayBeforeCredits = new Vector2(2.0f, 4.0f);

    [Header("The Final Trigger")]
    public UnityEvent onFinalMinigameCompleted;

    [Header("Live Debug (Watch in Play Mode)")]
    public float currentProgress = 0f;
    public bool isMinigameComplete = false;


    // Internal variables
    private float currentDriftInterval;
    private float currentTargetAmplitude, currentTargetFrequency, currentTargetPhase;
    private float oldTargetAmplitude, oldTargetFrequency, oldTargetPhase;
    private float newTargetAmplitude, newTargetFrequency, newTargetPhase;
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
            lightRenderer.material = lightOffMaterial;

        for (int i = 0; i < progressLEDs.Length; i++)
        {
            if (progressLEDs[i] != null && lightOffMaterial != null) progressLEDs[i].material = lightOffMaterial;
            if (i < progressPointLights.Length && progressPointLights[i] != null) progressPointLights[i].intensity = 0f;
        }

        if (dataTransmittedText != null) dataTransmittedText.SetActive(false);

        this.enabled = false;
        if (targetLine) targetLine.gameObject.SetActive(false);
        if (playerLine) playerLine.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isMinigameComplete) return;

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
            case 0: newTargetAmplitude = Mathf.Clamp(currentTargetAmplitude + Random.Range(-amplitudeDriftVariance, amplitudeDriftVariance), 0.16f, 1.1f); break;
            case 1: newTargetFrequency = Mathf.Clamp(currentTargetFrequency + Random.Range(-frequencyDriftVariance, frequencyDriftVariance), 6.2f, 10.0f); break;
            case 2: newTargetPhase = Mathf.Clamp(currentTargetPhase + Random.Range(-phaseDriftVariance, phaseDriftVariance), 0f, 12.56f); break;
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

        if (debugForceSync || Input.GetKey(debugForceSyncKey)) isSynced = true;

        if (isSynced) currentProgress += Time.deltaTime;

        currentProgress = Mathf.Clamp(currentProgress, 0f, timeToComplete);

        UpdateProgressLEDs();

        if (lightRenderer != null) lightRenderer.material = isSynced ? (lightOnMaterial != null ? lightOnMaterial : lightRenderer.material) : (lightOffMaterial != null ? lightOffMaterial : lightRenderer.material);
        if (pointLightObject != null) pointLightObject.SetActive(isSynced);

        // --- WIN CONDITION ---
        if (currentProgress >= timeToComplete)
        {
            isMinigameComplete = true;
            Debug.Log("<color=cyan>FINAL SEQUENCE TRIGGERED!</color>");

            StartCoroutine(EndgameSequenceRoutine());
        }
    }

    void UpdateProgressLEDs()
    {
        if (progressLEDs == null || progressLEDs.Length == 0) return;

        float timePerLED = timeToComplete / progressLEDs.Length;
        int ledsDoneThisFrame = 0;

        for (int i = 0; i < progressLEDs.Length; i++)
        {
            float startThreshold = i * timePerLED;
            float endThreshold = (i + 1) * timePerLED;

            if (i == progressLEDs.Length - 1) endThreshold = timeToComplete;

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

    public void StartFinalMinigame()
    {
        if (isMinigameComplete) return;

        currentProgress = 0f;
        completedLEDCount = 0;
        currentTargetAmplitude = baseTargetAmplitude;
        currentTargetFrequency = baseTargetFrequency;
        currentTargetPhase = baseTargetPhase;
        currentDriftInterval = Random.Range(minDriftInterval, maxDriftInterval);
        PickNewTargets();

        if (targetLine) targetLine.gameObject.SetActive(true);
        if (playerLine) playerLine.gameObject.SetActive(true);
        this.enabled = true;

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

    private void TurnOffMachine(bool playSound = true)
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

        if (playSound)
        {
            if (crtLoopSource != null) crtLoopSource.Stop();
            if (crtOneShotSource != null) crtOneShotSource.pitch = 1f;
            if (crtOneShotSource != null && crtTurnOffSound != null)
            {
                crtOneShotSource.PlayOneShot(crtTurnOffSound);
            }
        }
        else
        {
            if (crtLoopSource != null) crtLoopSource.Stop();
        }
    }

    // ==========================================
    // --- THE GRAND FINALE SEQUENCE ---
    // ==========================================
    private IEnumerator EndgameSequenceRoutine()
    {
        // ... (Keep Steps 1-4 exactly the same) ...
        if (targetLine) targetLine.gameObject.SetActive(false);
        if (playerLine) playerLine.gameObject.SetActive(false);
        if (lightRenderer != null && lightOffMaterial != null) lightRenderer.material = lightOffMaterial;
        if (pointLightObject != null) pointLightObject.SetActive(false);

        // Step 1: Blink the text using inspector timing
        if (dataTransmittedText != null)
        {
            for (int i = 0; i < 5; i++)
            {
                dataTransmittedText.SetActive(true);
                yield return new WaitForSeconds(textBlinkInterval);
                dataTransmittedText.SetActive(false);
                yield return new WaitForSeconds(textBlinkInterval);
            }

            // Step 2: Keep it solid
            dataTransmittedText.SetActive(true);
            yield return new WaitForSeconds(textSolidDuration);
            dataTransmittedText.SetActive(false);
        }

        // Step 3: Turn off the machine
        TurnOffMachine(true);
        yield return new WaitForSeconds(delayBeforeScreech);

        // Step 4: The Screech Outside
        if (externalScreechSource != null && monsterScreechClip != null)
        {
            externalScreechSource.PlayOneShot(monsterScreechClip);
        }
        yield return new WaitForSeconds(delayAfterScreech);

        // Step 5: Flicker the Shed Lights AND Bulb Meshes
        int flickerCount = LightFlickerAmount;
        for (int i = 0; i < flickerCount; i++)
        {
            SetShedLights(false);
            if (lightFlickerSource && fastFlickerClip) lightFlickerSource.PlayOneShot(fastFlickerClip);
            yield return new WaitForSeconds(Random.Range(flickerOffDuration.x, flickerOffDuration.y));

            SetShedLights(true);
            if (lightFlickerSource && fastFlickerClip) lightFlickerSource.PlayOneShot(fastFlickerClip);
            yield return new WaitForSeconds(Random.Range(flickerOnDuration.x, flickerOnDuration.y));
        }

        // Step 6: Pitch Black Power Down
        SetShedLights(false);
        if (lightFlickerSource && finalPowerDownClip) lightFlickerSource.PlayOneShot(finalPowerDownClip);

        // --- NEW: THE INTRUDER SEQUENCE ---

        // Step 7: The Initial Silence
        yield return new WaitForSeconds(silenceBeforeIntruder);

        // Step 8: The Crawl
        if (intruderAudioSource != null && intruderStartNode != null && intruderEndNode != null && intruderLoopClip != null)
        {
            // Snap the audio source to the start position
            intruderAudioSource.transform.position = intruderStartNode.position;

            // Set up and play the loop
            intruderAudioSource.clip = intruderLoopClip;
            intruderAudioSource.loop = true;
            intruderAudioSource.volume = intruderVolume;
            intruderAudioSource.Play();

            // Lerp the position over time
            float travelTimer = 0f;
            while (travelTimer < intruderTravelDuration)
            {
                travelTimer += Time.deltaTime;
                float percent = travelTimer / intruderTravelDuration;

                intruderAudioSource.transform.position = Vector3.Lerp(intruderStartNode.position, intruderEndNode.position, percent);

                yield return null;
            }

            // Snap the audio off instantly when it reaches the end
            intruderAudioSource.Stop();
        }
        else
        {
            Debug.LogWarning("Intruder sequence skipped: Missing AudioSource, Nodes, or Clip in Inspector.");
        }
        // --- NEW: Step 8.5: The Forced Look ---
        if (playerCamera != null && intruderEndNode != null)
        {
            if (fpsController != null) fpsController.enabled = false;
            Quaternion startRot = playerCamera.rotation;
            // Calculate the exact angle needed to look directly at the end node
            Quaternion targetRot = Quaternion.LookRotation(intruderEndNode.position - playerCamera.position);

            float snapTimer = 0f;
            while (snapTimer < cameraSnapDuration)
            {
                snapTimer += Time.deltaTime;
                playerCamera.rotation = Quaternion.Slerp(startRot, targetRot, snapTimer / cameraSnapDuration);
                yield return null;
            }
            // Guarantee it is perfectly locked on at the end of the timer
            playerCamera.rotation = targetRot;
        }

        // Step 9: The Final Breath
        yield return new WaitForSeconds(silenceBeforeCredits);

        // Step 10: Roll Credits!
        Debug.Log("<color=magenta>ROLL CREDITS!</color>");
        onFinalMinigameCompleted?.Invoke();
    }
    private void SetShedLights(bool isOn)
    {
        // 1. Toggle the actual Spotlight beams
        if (shedSpotlights != null)
        {
            foreach (Light l in shedSpotlights)
            {
                if (l != null) l.enabled = isOn;
            }
        }

        // 2. Toggle the Emission checkmark on the bulb meshes
        if (shedBulbRenderers != null)
        {
            foreach (MeshRenderer r in shedBulbRenderers)
            {
                if (r != null && r.material != null)
                {
                    if (isOn)
                    {
                        r.material.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        r.material.DisableKeyword("_EMISSION");
                    }
                }
            }
        }
    }
}