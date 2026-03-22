using UnityEngine;
using UnityEngine.Audio; // <-- REQUIRED FOR AUDIO SNAPSHOTS

public class MonsterDirector : MonoBehaviour
{
    public enum EncounterState { Idle, GracePeriod, Approach, Silence, Strike, Siege, Retreat, ClutchStruggle }
    public enum StrikeType { Normal, PointBlank, Ambush, FogStrike }

    [Header("Dependencies")]
    [Tooltip("The active profile. (Will be auto-assigned from the Progression array)")]
    public DifficultyProfile currentDifficulty;
    public WinchController winchController;
    public JumpscareController jumpscareController;
    public ClutchController clutchController;
    public RadioAudioController radioAudio; // <-- AUDIO LINK

    [Header("Audio System (The Deafening Silence)")]
    public AudioMixerSnapshot normalAudioSnapshot;
    public AudioMixerSnapshot silenceAudioSnapshot;
    public float silenceFadeTime = 1.5f;

    [Header("Difficulty Progression")]
    [Tooltip("Place your difficulty profiles here in order (Easy to Hard).")]
    public DifficultyProfile[] difficultyProgression;
    public int currentProgressionIndex = 0;

    [Header("Monster Physical Spawning")]
    public Transform monsterTransform;
    public Animator monsterAnimator; // <--- NEW: The Animation Bridge
    public Transform rampEntryTarget;
    public float spawnRadius = 25f;
    public Vector2 spawnAngleClamp = new Vector2(-45f, 45f);

    [Header("Clutch System Settings")]
    public float clutchCutoffAngle = -133.3f;
    public float siegePeekSilenceDuration = 0.8f;

    [Header("Live Debug")]
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
    private float currentSprintSpeed; // <--- ADD THIS LINE BACK HERE

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
                else if (winchController != null && winchController.IsDoorClosed) stateTimer = currentDifficulty.GetRandomizedTimer(currentDifficulty.baseGracePeriod);
                else
                {
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0) TransitionToState(EncounterState.Approach);
                }
                break;

            case EncounterState.Approach:
                if (isMinigameComplete)
                {
                    TransitionToState(EncounterState.Retreat); // <--- FAST TRACK TO RETREAT
                }
                else
                {
                    stateTimer -= Time.deltaTime;

                    // --- AUDIO HOOK: Fading static IN ---
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

                if (isMinigameComplete)
                {
                    TransitionToState(EncounterState.Retreat); // <--- FAST TRACK TO RETREAT
                }
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
                if (isMinigameComplete)
                {
                    TransitionToState(EncounterState.Retreat); // <--- FAST TRACK TO RETREAT
                }
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
                        // --- THE FIX: Check if it's fully closed FIRST! ---
                        if (winchController != null && winchController.IsDoorClosed)
                        {
                            TransitionToState(EncounterState.Siege);
                        }
                        else if (winchController != null && winchController.CurrentAngle > clutchCutoffAngle)
                        {
                            TransitionToState(EncounterState.ClutchStruggle);
                        }
                        else
                        {
                            TriggerJumpscare();
                        }
                    }
                }
                break;

            case EncounterState.Siege:
                if (isMinigameComplete)
                {
                    TransitionToState(EncounterState.Retreat); // <--- FAST TRACK TO RETREAT
                }
                else if (winchController != null && winchController.CurrentAngle < winchController.closedAngle - 5f)
                {
                    Debug.Log("<color=red>MONSTER: SIEGE BREACHED! Player opened the door!</color>");
                    TransitionToState(EncounterState.Strike);
                }
                else
                {
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0) TransitionToState(EncounterState.Retreat);
                }
                break;

            case EncounterState.Retreat:
                stateTimer -= Time.deltaTime;

                // --- AUDIO HOOK: Fading static OUT as it retreats ---
                if (radioAudio != null && currentMaxRetreatTime > 0f)
                {
                    float progress = stateTimer / currentMaxRetreatTime; // Goes from 1.0 down to 0.0
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
                if (monsterAnimator != null) monsterAnimator.SetBool("isLeaping", false); // <--- NEW: Reset Animation
                playerReactionTime = -1f;
                reactionStopwatch = 0f;
                if (normalAudioSnapshot != null) normalAudioSnapshot.TransitionTo(silenceFadeTime);
                if (radioAudio != null) radioAudio.isMonsterApproaching = false;
                radioAudio.isMonsterRetreating = false;
                break;

            case EncounterState.GracePeriod:
                stateTimer = currentDifficulty.GetRandomizedTimer(currentDifficulty.baseGracePeriod);
                if (monsterTransform != null) monsterTransform.gameObject.SetActive(false);
                if (monsterAnimator != null) monsterAnimator.SetBool("isLeaping", false); // <--- NEW: Reset Animation
                playerReactionTime = -1f;
                reactionStopwatch = 0f;
                if (normalAudioSnapshot != null) normalAudioSnapshot.TransitionTo(silenceFadeTime);
                if (radioAudio != null) radioAudio.isMonsterApproaching = false;
                radioAudio.isMonsterRetreating = false;
                break;

            case EncounterState.Approach:
                if (!isReversing)
                {
                    currentMaxApproachTime = currentDifficulty.GetRandomizedTimer(currentDifficulty.approachDuration);
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
                    currentMaxSilenceTime = currentDifficulty.GetRandomizedTimer(currentDifficulty.silenceDuration);
                    stateTimer = currentMaxSilenceTime;
                }
                else
                {
                    stateTimer = 0f;
                }
                reactionStopwatch = 0f;

                // --- THE SUCK: Audio goes totally dead ---
                if (silenceAudioSnapshot != null) silenceAudioSnapshot.TransitionTo(silenceFadeTime);
                if (radioAudio != null) radioAudio.isMonsterApproaching = false;
                radioAudio.isMonsterRetreating = false;
                break;

            case EncounterState.Strike:
                stateTimer = currentDifficulty.strikeDuration;

                bool isSiegeBreach = (previousState == EncounterState.Siege);

                float distanceToPlayer = Vector3.Distance(rampEntryTarget.position, playerCamera.position);
                Vector3 rampChestLevel = rampEntryTarget.position + (Vector3.up * 1.0f);

                if (distanceToPlayer < pointBlankThreshold)
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

                // Removed the normalAudioSnapshot.TransitionTo() here so it stays silent!
                break;

            case EncounterState.ClutchStruggle:
                if (monsterTransform != null) monsterTransform.gameObject.SetActive(false);

                if (clutchController != null)
                {
                    float maxDangerTime = currentMaxSilenceTime + currentDifficulty.strikeDuration;
                    clutchController.EvaluateStruggle(playerReactionTime, maxDangerTime, winchController.CurrentAngle, currentDifficulty);
                }
                break;

            case EncounterState.Siege:
                stateTimer = currentDifficulty.GetRandomizedTimer(currentDifficulty.patienceThreshold);

                // --- AUDIO HOOK: Bring the room tone back while it waits outside ---
                if (normalAudioSnapshot != null) normalAudioSnapshot.TransitionTo(silenceFadeTime);
                break;

            case EncounterState.Retreat:
                currentMaxRetreatTime = currentDifficulty.GetRandomizedTimer(currentDifficulty.retreatDuration);
                stateTimer = currentMaxRetreatTime;

                // --- VISUAL HOOK: Make the physical body vanish if banished mid-sprint ---
                if (monsterTransform != null) monsterTransform.gameObject.SetActive(false);
                if (monsterAnimator != null) monsterAnimator.SetBool("isLeaping", false); // <--- NEW: Reset Animation

                // --- AUDIO HOOK: Turn the static back on to signify it leaving ---
                if (normalAudioSnapshot != null) normalAudioSnapshot.TransitionTo(silenceFadeTime);
                if (radioAudio != null)
                {
                    radioAudio.isMonsterApproaching = true;
                    radioAudio.isMonsterRetreating = true;
                    radioAudio.approachProgress = 1f;
                }
                break;
        }
    }

    private void SpawnAndCalculateMonsterSprint(Transform target, bool forceExtremeAngle = false)
    {
        if (monsterTransform == null || target == null) return;

        float spawnAngle;
        if (forceExtremeAngle)
        {
            spawnAngle = (Random.value > 0.5f) ? spawnAngleClamp.x : spawnAngleClamp.y;
        }
        else
        {
            spawnAngle = Random.Range(spawnAngleClamp.x, spawnAngleClamp.y);
        }

        Vector3 direction = Quaternion.Euler(0, spawnAngle, 0) * target.forward;
        direction.y = 0;

        Vector3 spawnPos = target.position + (direction.normalized * spawnRadius);
        spawnPos.y = rampEntryTarget.position.y;
        monsterTransform.position = spawnPos;

        Vector3 lookTarget = new Vector3(target.position.x, monsterTransform.position.y, target.position.z);
        monsterTransform.LookAt(lookTarget);

        monsterTransform.gameObject.SetActive(true);

        float flatDistance = Vector2.Distance(new Vector2(monsterTransform.position.x, monsterTransform.position.z), new Vector2(target.position.x, target.position.z));
        currentSprintSpeed = flatDistance / stateTimer;
    }

    private void TriggerJumpscare()
    {
        isEncounterActive = false;
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

        if (currentProgressionIndex >= difficultyProgression.Length)
        {
            currentProgressionIndex = difficultyProgression.Length - 1;
            Debug.Log("<color=magenta>MONSTER PROGRESSION: Maximum difficulty reached!</color>");
        }
        else
        {
            Debug.Log($"<color=magenta>MONSTER PROGRESSION: Difficulty increased to Profile {currentProgressionIndex + 1}/{difficultyProgression.Length}</color>");
        }

        currentDifficulty = difficultyProgression[currentProgressionIndex];
    }
}