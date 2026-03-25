using UnityEngine;
using UnityEngine.Events; // <--- Needed for the Endgame Trigger

public class FinalCRTWaveController : MonoBehaviour
{
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
    public MeshRenderer lightRenderer;
    public GameObject pointLightObject;

    [Header("Progress LEDs")]
    public MeshRenderer[] progressLEDs = new MeshRenderer[3];
    public Light[] progressPointLights = new Light[3];
    public float maxProgressLightIntensity = 1.0f;
    public Material lightOffMaterial;
    public Material lightOnMaterial;

    [Header("=== FINAL PUZZLE DIFFICULTY ===")]
    [Tooltip("How long the player must hold the sync to win the game.")]
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

    [Header("=== ENDGAME TRIGGER ===")]
    [Tooltip("What happens when the final wave is synced? (E.g. Turn screen white, play loud sound, load credits)")]
    public UnityEvent onFinalMinigameCompleted;

    [Header("Live Debug (Watch in Play Mode)")]
    public float currentProgress = 0f;
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

        // Ensure the machine starts completely off
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

        if (isSynced) currentProgress += Time.deltaTime;

        currentProgress = Mathf.Clamp(currentProgress, 0f, timeToComplete);

        UpdateProgressLEDs();

        if (lightRenderer != null)
        {
            lightRenderer.material = isSynced ? (lightOnMaterial != null ? lightOnMaterial : lightRenderer.material) : (lightOffMaterial != null ? lightOffMaterial : lightRenderer.material);
        }
        if (pointLightObject != null) pointLightObject.SetActive(isSynced);

        // --- WIN CONDITION ---
        if (currentProgress >= timeToComplete)
        {
            isMinigameComplete = true;
            Debug.Log("<color=cyan>FINAL SEQUENCE TRIGGERED!</color>");

            // Turn off the machine visuals
            TurnOffMachine(false);

            // Fire the ending events!
            onFinalMinigameCompleted?.Invoke();
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

    // --- CALL THIS FROM YOUR PUNCHCARD READER TO START THE FINAL PUZZLE ---
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

        Debug.Log("<color=green>FINAL CRT MACHINE ONLINE.</color>");
    }

    private void TurnOffMachine(bool playSound = true)
    {
        this.enabled = false;
        crtOneShotSource.PlayOneShot(crtTurnOffSound);


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
                Debug.Log("<color=red>FINAL CRT MACHINE OFFLINE.</color>");
            }
        }
        else
        {
            // Just kill the loop silently for the ending transition
            if (crtLoopSource != null) crtLoopSource.Stop();
        }
    }
}