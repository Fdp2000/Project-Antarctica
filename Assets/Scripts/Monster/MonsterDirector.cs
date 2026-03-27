using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class MonsterDirector : MonoBehaviour
{
    public enum EncounterState { Idle, GracePeriod, Approach, Silence, Strike, Siege, Retreat, ClutchStruggle }
    public enum StrikeType { Normal, PointBlank, Ambush, FogStrike, StalkBehind }

    [Header("Dependencies")]
    public DifficultyProfile currentDifficulty;
    public WinchController winchController;
    public JumpscareController jumpscareController;
    public ClutchController clutchController;
    public RadioAudioController radioAudio;

    [Header("Audio System (The Deafening Silence)")]
    public AudioMixerSnapshot normalAudioSnapshot;
    public AudioMixerSnapshot silenceAudioSnapshot;
    public float silenceFadeTime = 1.5f;

    [Header("Difficulty Progression")]
    public DifficultyProfile[] difficultyProgression;
    public int currentProgressionIndex = 0;

    [Header("Monster Physical Spawning")]
    public Transform monsterTransform;
    public Animator monsterAnimator;
    public Transform rampEntryTarget;
    public float spawnRadius = 25f;
    public Vector2 spawnAngleClamp = new Vector2(-45f, 45f);
    public AudioSource footstepAudio; // <--- NEW: Footstep Audio Link

    [Header("Clutch System Settings")]
    public float clutchCutoffAngle = -133.3f;
    public float siegePeekSilenceDuration = 0.8f;

    [Header("Siege Events (Parallel System)")]
    [Tooltip("Event 1: Bang on the door.")]
    public AudioSource doorAudioSource;
    public AudioClip doorBangClip;

    [Tooltip("Event 2: Roar on the roof.")]
    public AudioSource roofAudioSource;
    public AudioClip roofRoarClip;

    [Tooltip("Event 3: Scrape along the side.")]
    public AudioSource sideScrapeAudioSource;
    public AudioClip metalScrapeClip;

    [Header("Scrape Paths")]
    public Transform scrapeLeftStart;
    public Transform scrapeLeftEnd;
    public Transform scrapeRightStart;
    public Transform scrapeRightEnd;

    [Tooltip("Camera shake settings for Bangs and Roars.")]
    public float shakeMagnitude = 0.15f;
    public float shakeDuration = 0.3f;

    private float siegeEventTimer = 0f;
    private Coroutine scrapeCoroutine;
    private Coroutine shakeCoroutine;

    [Header("Live Debug")]
    public bool debugForceAmbush = false;
    public bool debugForceDoorBang = false;
    public bool debugForceRoofRoar = false;
    public bool debugForceMetalScrape = false;

    public EncounterState currentState = EncounterState.Idle;
    public StrikeType activeStrikeType = StrikeType.Normal;
    public float stateTimer = 0f;
    public bool isEncounterActive = false;
    public bool isMinigameComplete = false;

    [Header("Clutch Data (Live)")]
    public float reactionStopwatch = 0f;
    public float playerReactionTime = -1f;

    private float currentMaxApproachTime;
    private float currentMaxSilenceTime;
    private float currentMaxRetreatTime;
    private float currentMaxStrikeTime; // <--- ADD THIS
    private float currentSprintSpeed;

    [Header("Strike Logic Funnel")]
    public Transform playerCamera;
    public float pointBlankThreshold = 2.0f;
    public bool isPlayerInCabin = true;
    public LayerMask obstacleLayers;

    void Start()
    {
        if (difficultyProgression != null && difficultyProgression.Length > 0)
        {
            currentProgressionIndex = 0;
            currentDifficulty = difficultyProgression[currentProgressionIndex];
        }

        if (winchController != null) winchController.OnDoorStartedClosing += RecordPlayerReaction;

        // --- NEW: Set the initial state of the world ---
        if (POIDirector.Instance != null) POIDirector.Instance.EvaluatePOIs(currentProgressionIndex);
    }

    void OnDestroy()
    {
        if (winchController != null) winchController.OnDoorStartedClosing -= RecordPlayerReaction;
    }

    private void RecordPlayerReaction()
    {
        if (playerReactionTime >= 0f) return;

        if (currentState == EncounterState.Approach || currentState == EncounterState.GracePeriod)
        {
            playerReactionTime = 0f;
            Debug.Log("<color=green>MONSTER: Player reacted early! Perfect reaction score locked in.</color>");
        }
        else if (currentState == EncounterState.Silence || currentState == EncounterState.Strike)
        {
            playerReactionTime = reactionStopwatch;
            Debug.Log($"<color=yellow>MONSTER: Player reaction locked in at {playerReactionTime:F2}s into the Danger Zone.</color>");
        }
    }

    void Update()
    {
        if (!isEncounterActive) return;

        switch (currentState)
        {
            case EncounterState.GracePeriod:
                if (isMinigameComplete) TransitionToState(EncounterState.Idle);
                else if (winchController != null && winchController.IsDoorClosed) stateTimer = currentDifficulty.GetRandomTimer(currentDifficulty.gracePeriod);
                else
                {
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0) TransitionToState(EncounterState.Approach);
                }
                break;

            case EncounterState.Approach:
                if (isMinigameComplete) TransitionToState(EncounterState.Retreat);
                else
                {
                    stateTimer -= Time.deltaTime;
                    if (radioAudio != null)
                    {
                        float progress = 1f - (stateTimer / currentMaxApproachTime);
                        radioAudio.approachProgress = progress;
                    }
                    if (stateTimer <= 0) TransitionToState(EncounterState.Silence);
                }
                break;

            case EncounterState.Silence:
                reactionStopwatch += Time.deltaTime;
                if (isMinigameComplete) TransitionToState(EncounterState.Retreat);
                else
                {
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0)
                    {
                        if (winchController != null && winchController.IsDoorClosed)
                            TransitionToState(EncounterState.Siege);
                        else
                            TransitionToState(EncounterState.Strike);
                    }
                }
                break;

            case EncounterState.Strike:
                if (isMinigameComplete) TransitionToState(EncounterState.Retreat);
                else
                {
                    reactionStopwatch += Time.deltaTime;
                    stateTimer -= Time.deltaTime;

                    if (monsterTransform != null)
                    {
                        Transform currentTarget = (activeStrikeType == StrikeType.FogStrike || activeStrikeType == StrikeType.PointBlank) ? playerCamera : rampEntryTarget;
                        Vector3 flatTarget = new Vector3(currentTarget.position.x, monsterTransform.position.y, currentTarget.position.z);
                        monsterTransform.position = Vector3.MoveTowards(monsterTransform.position, flatTarget, currentSprintSpeed * Time.deltaTime);
                        monsterTransform.LookAt(flatTarget);
                    }

                    if (stateTimer <= 0)
                    {
                        CRTWaveController crt = FindObjectOfType<CRTWaveController>();
                        if (crt != null) crt.isSignalBlockedByMonster = true;

                        if (winchController != null && winchController.IsDoorClosed) TransitionToState(EncounterState.Siege);
                        else if (winchController != null && winchController.CurrentAngle > clutchCutoffAngle) TransitionToState(EncounterState.ClutchStruggle);
                        else TriggerJumpscare();
                    }
                }
                break;

            case EncounterState.Siege:
                if (isMinigameComplete) TransitionToState(EncounterState.Retreat);
                else if (winchController != null && winchController.CurrentAngle < winchController.closedAngle - 5f)
                {
                    Debug.Log("<color=red>MONSTER: SIEGE BREACHED! Player opened the door!</color>");
                    TransitionToState(EncounterState.Strike);
                }
                else
                {
                    stateTimer -= Time.deltaTime;

                    siegeEventTimer -= Time.deltaTime;
                    if (siegeEventTimer <= 0f)
                    {
                        TriggerRandomSiegeEvent();
                        siegeEventTimer = Random.Range(currentDifficulty.siegeEventInterval.x, currentDifficulty.siegeEventInterval.y);
                    }

                    if (stateTimer <= 0) TransitionToState(EncounterState.Retreat);
                }
                break;

            case EncounterState.Retreat:
                stateTimer -= Time.deltaTime;
                if (radioAudio != null && currentMaxRetreatTime > 0f)
                {
                    float progress = stateTimer / currentMaxRetreatTime;
                    radioAudio.approachProgress = Mathf.Clamp01(progress);
                }
                if (stateTimer <= 0) TransitionToState(EncounterState.GracePeriod);
                break;
        }
    }

    public void TransitionToState(EncounterState newState, bool isReversing = false)
    {
        EncounterState previousState = currentState;
        currentState = newState;

        if (currentDifficulty == null) return;

        switch (newState)
        {
            case EncounterState.Idle:
                isEncounterActive = false;
                if (monsterTransform != null) monsterTransform.gameObject.SetActive(false);
                if (footstepAudio != null) footstepAudio.Stop(); // <--- NEW: Stop Footsteps
                playerReactionTime = -1f;
                reactionStopwatch = 0f;
                if (normalAudioSnapshot != null) normalAudioSnapshot.TransitionTo(silenceFadeTime);
                if (radioAudio != null) radioAudio.isMonsterApproaching = false;
                radioAudio.isMonsterRetreating = false;
                CRTWaveController safeCrt = FindObjectOfType<CRTWaveController>();
                if (safeCrt != null) safeCrt.isSignalBlockedByMonster = false;
                break;

            case EncounterState.GracePeriod:
                stateTimer = currentDifficulty.GetRandomTimer(currentDifficulty.gracePeriod);
                if (monsterTransform != null) monsterTransform.gameObject.SetActive(false);
                if (footstepAudio != null) footstepAudio.Stop(); // <--- NEW: Stop Footsteps
                if (monsterAnimator != null) monsterAnimator.SetBool("isLeaping", false);
                playerReactionTime = -1f;
                reactionStopwatch = 0f;
                if (normalAudioSnapshot != null) normalAudioSnapshot.TransitionTo(silenceFadeTime);
                if (radioAudio != null) radioAudio.isMonsterApproaching = false;
                radioAudio.isMonsterRetreating = false;
                CRTWaveController scrt = FindObjectOfType<CRTWaveController>();
                if (scrt != null) scrt.isSignalBlockedByMonster = false;
                break;

            case EncounterState.Approach:
                if (!isReversing)
                {
                    currentMaxApproachTime = currentDifficulty.GetRandomTimer(currentDifficulty.approachDuration);
                    stateTimer = currentMaxApproachTime;
                }
                else stateTimer = 0f;

                if (normalAudioSnapshot != null) normalAudioSnapshot.TransitionTo(silenceFadeTime);
                if (radioAudio != null)
                {
                    radioAudio.isMonsterApproaching = true;
                    radioAudio.isMonsterRetreating = false;
                    radioAudio.approachProgress = 0f;
                }
                break;

            case EncounterState.Silence:
                if (!isReversing)
                {
                    currentMaxSilenceTime = currentDifficulty.GetRandomTimer(currentDifficulty.silenceDuration);
                    stateTimer = currentMaxSilenceTime;
                }
                else stateTimer = 0f;
                reactionStopwatch = 0f;
                if (silenceAudioSnapshot != null) silenceAudioSnapshot.TransitionTo(silenceFadeTime);
                if (radioAudio != null) radioAudio.isMonsterApproaching = false;
                radioAudio.isMonsterRetreating = false;
                break;

            case EncounterState.Strike:
                currentMaxStrikeTime = currentDifficulty.GetRandomTimer(currentDifficulty.strikeDuration);
                stateTimer = currentMaxStrikeTime;
                bool isSiegeBreach = (previousState == EncounterState.Siege);
                float distanceToPlayer = Vector3.Distance(rampEntryTarget.position, playerCamera.position);
                Vector3 rampChestLevel = rampEntryTarget.position + (Vector3.up * 1.0f);

                if (debugForceAmbush)
                {
                    activeStrikeType = StrikeType.Ambush;
                    SpawnAndCalculateMonsterSprint(rampEntryTarget, isSiegeBreach);
                }
                else if (distanceToPlayer < pointBlankThreshold)
                {
                    activeStrikeType = StrikeType.PointBlank;
                    SpawnAndCalculateMonsterSprint(playerCamera, isSiegeBreach);
                }
                else if (!isPlayerInCabin)
                {
                    activeStrikeType = StrikeType.FogStrike;
                    SpawnAndCalculateMonsterSprint(playerCamera, isSiegeBreach);
                }
                else if (Physics.Linecast(rampChestLevel, playerCamera.position, obstacleLayers))
                {
                    activeStrikeType = StrikeType.Ambush;
                    SpawnAndCalculateMonsterSprint(rampEntryTarget, isSiegeBreach);
                }
                else
                {
                    activeStrikeType = StrikeType.Normal;
                    SpawnAndCalculateMonsterSprint(rampEntryTarget, isSiegeBreach);
                }
                break;

            case EncounterState.ClutchStruggle:
                if (monsterTransform != null) monsterTransform.gameObject.SetActive(false);
                if (footstepAudio != null) footstepAudio.Stop(); // <--- NEW: Stop Footsteps for Clutch
                if (clutchController != null)
                {
                    float maxDangerTime = currentMaxSilenceTime + currentMaxStrikeTime;
                    clutchController.EvaluateStruggle(playerReactionTime, maxDangerTime, winchController.CurrentAngle, currentDifficulty);
                }
                break;

            case EncounterState.Siege:
                stateTimer = currentDifficulty.GetRandomTimer(currentDifficulty.patienceThreshold);
                if (footstepAudio != null) footstepAudio.Stop();
                siegeEventTimer = Random.Range(currentDifficulty.siegeEventInterval.x, currentDifficulty.siegeEventInterval.y);
                if (silenceAudioSnapshot != null) silenceAudioSnapshot.TransitionTo(silenceFadeTime);
                break;

            case EncounterState.Retreat:
                currentMaxRetreatTime = currentDifficulty.GetRandomTimer(currentDifficulty.retreatDuration);
                stateTimer = currentMaxRetreatTime;
                if (monsterTransform != null) monsterTransform.gameObject.SetActive(false);
                if (footstepAudio != null) footstepAudio.Stop(); // <--- NEW: Stop Footsteps
                if (monsterAnimator != null) monsterAnimator.SetBool("isLeaping", false);
                if (normalAudioSnapshot != null) normalAudioSnapshot.TransitionTo(silenceFadeTime);
                CRTWaveController crt = FindObjectOfType<CRTWaveController>();
                if (crt != null) crt.isSignalBlockedByMonster = false;
                if (radioAudio != null)
                {
                    radioAudio.isMonsterApproaching = true;
                    radioAudio.isMonsterRetreating = true;
                    radioAudio.approachProgress = 1f;
                }
                break;
        }
    }

    private void TriggerRandomSiegeEvent()
    {
        int rand = Random.Range(0, 3);

        if (debugForceDoorBang) rand = 0;
        else if (debugForceRoofRoar) rand = 1;
        else if (debugForceMetalScrape) rand = 2;

        if (rand == 0)
        {
            Debug.Log("<color=red>SIEGE EVENT: DOOR BANG</color>");
            if (doorAudioSource != null && doorBangClip != null) doorAudioSource.PlayOneShot(doorBangClip);
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(CameraShakeRoutine(shakeDuration, shakeMagnitude));
        }
        else if (rand == 1)
        {
            Debug.Log("<color=red>SIEGE EVENT: ROOF ROAR</color>");
            if (roofAudioSource != null && roofRoarClip != null) roofAudioSource.PlayOneShot(roofRoarClip);
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(CameraShakeRoutine(shakeDuration, shakeMagnitude));
        }
        else
        {
            Debug.Log("<color=red>SIEGE EVENT: METAL SCRAPE</color>");
            if (sideScrapeAudioSource != null && metalScrapeClip != null)
            {
                bool goLeft = Random.value > 0.5f;
                Transform chosenStart = goLeft ? scrapeLeftStart : scrapeRightStart;
                Transform chosenEnd = goLeft ? scrapeLeftEnd : scrapeRightEnd;

                if (chosenStart != null && chosenEnd != null)
                {
                    if (scrapeCoroutine != null) StopCoroutine(scrapeCoroutine);
                    scrapeCoroutine = StartCoroutine(ScrapeRoutine(metalScrapeClip.length, chosenStart, chosenEnd));
                }
            }
        }
    }

    private IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        if (playerCamera == null) yield break;

        float elapsed = 0f;
        Vector3 originalLocalPos = playerCamera.localPosition;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            playerCamera.localPosition = originalLocalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerCamera.localPosition = originalLocalPos;
    }

    private IEnumerator ScrapeRoutine(float duration, Transform startNode, Transform endNode)
    {
        sideScrapeAudioSource.transform.position = startNode.position;
        sideScrapeAudioSource.PlayOneShot(metalScrapeClip);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            sideScrapeAudioSource.transform.position = Vector3.Lerp(startNode.position, endNode.position, percent);
            yield return null;
        }
    }

    private void SpawnAndCalculateMonsterSprint(Transform target, bool forceExtremeAngle = false)
    {
        if (monsterTransform == null || target == null) return;

        float spawnAngle = forceExtremeAngle ? ((Random.value > 0.5f) ? spawnAngleClamp.x : spawnAngleClamp.y) : Random.Range(spawnAngleClamp.x, spawnAngleClamp.y);
        Vector3 direction = Quaternion.Euler(0, spawnAngle, 0) * target.forward;
        direction.y = 0;

        Vector3 spawnPos = target.position + (direction.normalized * spawnRadius);
        spawnPos.y = rampEntryTarget.position.y;
        monsterTransform.position = spawnPos;

        Vector3 lookTarget = new Vector3(target.position.x, monsterTransform.position.y, target.position.z);
        monsterTransform.LookAt(lookTarget);

        monsterTransform.gameObject.SetActive(true);
        if (footstepAudio != null) footstepAudio.Play(); // <--- NEW: Start Footsteps!

        float flatDistance = Vector2.Distance(new Vector2(monsterTransform.position.x, monsterTransform.position.z), new Vector2(target.position.x, target.position.z));
        currentSprintSpeed = flatDistance / stateTimer;
    }

    private void TriggerJumpscare()
    {
        isEncounterActive = false;
        if (footstepAudio != null) footstepAudio.Stop(); // <--- NEW: Stop Footsteps exactly when leap/stalk begins!
        if (jumpscareController != null) jumpscareController.ExecuteJumpscare(activeStrikeType, monsterTransform, rampEntryTarget);
    }

    public void StartEncounter()
    {
        if (isEncounterActive) return;
        isEncounterActive = true;
        isMinigameComplete = false;
        TransitionToState(EncounterState.GracePeriod);
    }

    public void EndEncounter(bool playerWonMinigame)
    {
        if (!isEncounterActive || currentState == EncounterState.ClutchStruggle) return;

        if (playerWonMinigame && !isMinigameComplete)
        {
            isMinigameComplete = true;
            AdvanceDifficulty();
        }
    }

    public void AdvanceDifficulty()
    {
        if (difficultyProgression == null || difficultyProgression.Length == 0) return;

        currentProgressionIndex++;
        if (currentProgressionIndex >= difficultyProgression.Length) currentProgressionIndex = difficultyProgression.Length - 1;
        currentDifficulty = difficultyProgression[currentProgressionIndex];

        // --- NEW: Unlock new POIs dynamically ---
        if (POIDirector.Instance != null) POIDirector.Instance.EvaluatePOIs(currentProgressionIndex);
    }
}