using UnityEngine;

public class MonsterDirector : MonoBehaviour
{
    public enum EncounterState { Idle, GracePeriod, Approach, Silence, Strike, Siege, Retreat, ClutchStruggle }
    public enum StrikeType { Normal, PointBlank, Ambush, FogStrike }

    [Header("Dependencies")]
    public DifficultyProfile currentDifficulty;
    public WinchController winchController;
    public JumpscareController jumpscareController;
    public ClutchController clutchController; // <--- NEW: The system we will build next!

    [Header("Monster Physical Spawning")]
    public Transform monsterTransform;
    public Transform rampEntryTarget;
    public float spawnRadius = 25f;
    public Vector2 spawnAngleClamp = new Vector2(-45f, 45f);

    [Header("Clutch System Settings")]
    public float clutchCutoffAngle = -133.3f; // Updated to your specific angle
    public float siegePeekSilenceDuration = 0.8f;

    [Header("Live Debug")]
    public EncounterState currentState = EncounterState.Idle;
    public StrikeType activeStrikeType = StrikeType.Normal;
    public float stateTimer = 0f;
    public bool isEncounterActive = false;
    public bool isMinigameComplete = false;

    // --- NEW: Danger Zone Tracking ---
    [Header("Clutch Data (Live)")]
    public float reactionStopwatch = 0f;
    public float playerReactionTime = -1f; // -1 means they haven't reacted yet

    private float currentMaxApproachTime;
    private float currentMaxSilenceTime;
    private float currentSprintSpeed;

    [Header("Strike Logic Funnel")]
    public Transform playerCamera;
    public float pointBlankThreshold = 2.0f;
    public bool isPlayerInCabin = true;
    public LayerMask obstacleLayers;

    void Start()
    {
        // Listen for the exact moment the player touches the winch
        if (winchController != null)
        {
            winchController.OnDoorStartedClosing += RecordPlayerReaction;
        }
    }

    void OnDestroy()
    {
        if (winchController != null)
        {
            winchController.OnDoorStartedClosing -= RecordPlayerReaction;
        }
    }

    // --- NEW: Captures the player's reflex speed ---
    // --- UPDATED: Captures the player's reflex speed ---
    private void RecordPlayerReaction()
    {
        // If we already recorded their reaction, ignore this
        if (playerReactionTime >= 0f) return;

        if (currentState == EncounterState.Approach || currentState == EncounterState.GracePeriod)
        {
            // The player reacted before the Danger Zone even started! Give them a perfect 0.0s time.
            playerReactionTime = 0f;
            Debug.Log("<color=green>MONSTER: Player reacted early! Perfect reaction score locked in.</color>");
        }
        else if (currentState == EncounterState.Silence || currentState == EncounterState.Strike)
        {
            // The player reacted during the Danger Zone. Record the exact stopwatch time.
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
                    stateTimer += Time.deltaTime;
                    if (stateTimer >= currentMaxApproachTime) TransitionToState(EncounterState.Idle);
                }
                else
                {
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0) TransitionToState(EncounterState.Silence);
                }
                break;

            case EncounterState.Silence:
                // --- NEW: Tick the Danger Zone Stopwatch ---
                reactionStopwatch += Time.deltaTime;

                if (isMinigameComplete)
                {
                    stateTimer += Time.deltaTime;
                    if (stateTimer >= currentMaxSilenceTime) TransitionToState(EncounterState.Approach, true);
                }
                else
                {
                    stateTimer -= Time.deltaTime;

                    if (stateTimer <= 0)
                    {
                        if (winchController != null && winchController.IsDoorClosed)
                            TransitionToState(EncounterState.Siege);
                        else
                            // The Silence is over. The monster ALWAYS strikes now.
                            TransitionToState(EncounterState.Strike);
                    }
                }
                break;

            case EncounterState.Strike:
                // --- NEW: Continue ticking the Danger Zone Stopwatch ---
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
                    // --- NEW: The Moment of Truth ---
                    // The monster hit the ramp. Can it fit through the door?
                    if (winchController != null && winchController.CurrentAngle > clutchCutoffAngle)
                    {
                        TransitionToState(EncounterState.ClutchStruggle);
                    }
                    else
                    {
                        // Door was too wide open. Instant death.
                        TriggerJumpscare();
                    }
                }
                break;

            case EncounterState.Siege:
                if (winchController != null && !winchController.IsDoorClosed) TransitionToState(EncounterState.Silence);
                else
                {
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0) TransitionToState(EncounterState.Retreat);
                }
                break;

            case EncounterState.Retreat:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0) TransitionToState(EncounterState.GracePeriod);
                break;
        }
    }

    private void TransitionToState(EncounterState newState, bool isReversing = false)
    {
        currentState = newState;
        if (currentDifficulty == null) return;

        switch (newState)
        {
            case EncounterState.Idle:
                isEncounterActive = false;
                if (monsterTransform != null) monsterTransform.gameObject.SetActive(false);
                playerReactionTime = -1f;
                reactionStopwatch = 0f;
                break;

            case EncounterState.GracePeriod:
                stateTimer = currentDifficulty.GetRandomizedTimer(currentDifficulty.baseGracePeriod);
                if (monsterTransform != null) monsterTransform.gameObject.SetActive(false);
                playerReactionTime = -1f;
                reactionStopwatch = 0f;
                break;

            case EncounterState.Approach:
                if (!isReversing)
                {
                    currentMaxApproachTime = currentDifficulty.GetRandomizedTimer(currentDifficulty.approachDuration);
                    stateTimer = currentMaxApproachTime;
                }
                else stateTimer = 0f;
                break;

            case EncounterState.Silence:
                currentMaxSilenceTime = currentDifficulty.GetRandomizedTimer(currentDifficulty.silenceDuration);
                stateTimer = currentMaxSilenceTime;

                // --- NEW: Reset the Danger Zone variables when Silence begins ---
                reactionStopwatch = 0f;
                break;

            case EncounterState.Strike:
                stateTimer = currentDifficulty.strikeDuration;

                float distanceToPlayer = Vector3.Distance(rampEntryTarget.position, playerCamera.position);
                Vector3 rampChestLevel = rampEntryTarget.position + (Vector3.up * 1.0f);

                if (distanceToPlayer < pointBlankThreshold)
                {
                    activeStrikeType = StrikeType.PointBlank;
                    SpawnAndCalculateMonsterSprint(playerCamera);
                }
                else if (!isPlayerInCabin)
                {
                    activeStrikeType = StrikeType.FogStrike;
                    SpawnAndCalculateMonsterSprint(playerCamera);
                }
                else if (Physics.Linecast(rampChestLevel, playerCamera.position, obstacleLayers))
                {
                    activeStrikeType = StrikeType.Ambush;
                    SpawnAndCalculateMonsterSprint(rampEntryTarget);
                }
                else
                {
                    activeStrikeType = StrikeType.Normal;
                    SpawnAndCalculateMonsterSprint(rampEntryTarget);
                }
                break;

            case EncounterState.ClutchStruggle:
                // --- NEW: Pass the data to the Clutch Controller to calculate the outcome ---
                if (clutchController != null)
                {
                    Debug.Log("<color=red><b>MONSTER: DOOR BLOCKED! INITIATING CLUTCH CALCULATION...</b></color>");

                    float maxDangerTime = currentMaxSilenceTime + currentDifficulty.strikeDuration;
                    clutchController.EvaluateStruggle(playerReactionTime, maxDangerTime, winchController.CurrentAngle, currentDifficulty);
                }
                else
                {
                    Debug.LogError("Missing ClutchController reference!");
                }
                break;

            case EncounterState.Siege:
                stateTimer = currentDifficulty.GetRandomizedTimer(currentDifficulty.patienceThreshold);
                break;

            case EncounterState.Retreat:
                stateTimer = currentDifficulty.GetRandomizedTimer(currentDifficulty.retreatDuration);
                break;
        }
    }

    private void SpawnAndCalculateMonsterSprint(Transform target)
    {
        if (monsterTransform == null || target == null) return;

        float randomAngle = Random.Range(spawnAngleClamp.x, spawnAngleClamp.y);
        Vector3 direction = Quaternion.Euler(0, randomAngle, 0) * target.forward;
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
        if (!isEncounterActive || currentState == EncounterState.Strike || currentState == EncounterState.ClutchStruggle) return;
        if (playerWonMinigame) isMinigameComplete = true;
    }
}