using UnityEngine;

public class MonsterDirector : MonoBehaviour
{
    public enum EncounterState { Idle, GracePeriod, Approach, Silence, Strike, Siege, Retreat, ClutchStruggle }
    public enum StrikeType { Normal, PointBlank, Ambush, FogStrike }

    [Header("Dependencies")]
    public DifficultyProfile currentDifficulty;
    public WinchController winchController;
    public JumpscareController jumpscareController; // <--- The new cinematic director

    [Header("Monster Physical Spawning")]
    public Transform monsterTransform;
    public Transform rampEntryTarget;
    public float spawnRadius = 25f;
    public Vector2 spawnAngleClamp = new Vector2(-45f, 45f);

    [Header("Clutch System Settings")]
    public float clutchCutoffAngle = -150f;
    public float siegePeekSilenceDuration = 0.8f;

    [Header("Live Debug")]
    public EncounterState currentState = EncounterState.Idle;
    public StrikeType activeStrikeType = StrikeType.Normal;
    public float stateTimer = 0f;
    public bool isEncounterActive = false;
    private float currentMaxApproachTime;
    private float currentSprintSpeed;

    [Header("Strike Logic Funnel")]
    public Transform playerCamera;
    public float pointBlankThreshold = 2.0f;
    public bool isPlayerInCabin = true;
    public LayerMask obstacleLayers;

    void Update()
    {
        if (!isEncounterActive) return;

        switch (currentState)
        {
            case EncounterState.GracePeriod:
                if (winchController != null && winchController.IsDoorClosed)
                {
                    stateTimer = currentDifficulty.GetRandomizedTimer(currentDifficulty.baseGracePeriod);
                }
                else
                {
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0) TransitionToState(EncounterState.Approach);
                }
                break;

            case EncounterState.Approach:
                if (winchController != null && winchController.IsDoorClosed)
                {
                    stateTimer += Time.deltaTime;
                    if (stateTimer >= currentMaxApproachTime) TransitionToState(EncounterState.GracePeriod);
                }
                else
                {
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0) TransitionToState(EncounterState.Silence);
                }
                break;

            case EncounterState.Silence:
                stateTimer -= Time.deltaTime;

                if (stateTimer <= 0)
                {
                    if (winchController != null && winchController.IsDoorClosed)
                        TransitionToState(EncounterState.Siege);
                    else if (winchController != null && winchController.CurrentAngle < clutchCutoffAngle)
                        TransitionToState(EncounterState.Strike);
                    else
                        TransitionToState(EncounterState.ClutchStruggle);
                }
                break;

            case EncounterState.Strike:
                stateTimer -= Time.deltaTime;

                if (monsterTransform != null)
                {
                    // Fog and PointBlank chase the player. Normal and Ambush chase the ramp.
                    Transform currentTarget = (activeStrikeType == StrikeType.FogStrike || activeStrikeType == StrikeType.PointBlank) ? playerCamera : rampEntryTarget;

                    Vector3 flatTarget = new Vector3(currentTarget.position.x, monsterTransform.position.y, currentTarget.position.z);
                    monsterTransform.position = Vector3.MoveTowards(monsterTransform.position, flatTarget, currentSprintSpeed * Time.deltaTime);
                    monsterTransform.LookAt(flatTarget);
                }

                // Hand off to the Jumpscare Controller!
                if (stateTimer <= 0) TriggerJumpscare();
                break;

            case EncounterState.Siege:
                if (winchController != null && !winchController.IsDoorClosed)
                {
                    TransitionToState(EncounterState.Silence);
                }
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

    private void TransitionToState(EncounterState newState)
    {
        currentState = newState;
        if (currentDifficulty == null) return;

        switch (newState)
        {
            case EncounterState.Idle:
                isEncounterActive = false;
                if (monsterTransform != null) monsterTransform.gameObject.SetActive(false);
                break;

            case EncounterState.GracePeriod:
                stateTimer = currentDifficulty.GetRandomizedTimer(currentDifficulty.baseGracePeriod);
                if (monsterTransform != null) monsterTransform.gameObject.SetActive(false);
                break;

            case EncounterState.Approach:
                currentMaxApproachTime = currentDifficulty.GetRandomizedTimer(currentDifficulty.approachDuration);
                stateTimer = currentMaxApproachTime;
                break;

            case EncounterState.Silence:
                stateTimer = currentDifficulty.GetRandomizedTimer(currentDifficulty.silenceDuration);
                break;

            case EncounterState.Strike:
                stateTimer = currentDifficulty.strikeDuration;

                // --- THE LOGIC FUNNEL (Evaluated BEFORE it starts sprinting) ---
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

                Debug.Log($"<color=red>MONSTER: SPOTTED! Sprinting for {stateTimer:F1}s! Type: {activeStrikeType}</color>");
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

        // Calculate the raw spawn position
        Vector3 spawnPos = target.position + (direction.normalized * spawnRadius);

        // Force the monster down to the snow level, regardless of who it is targeting
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

        // Hand off control to the new JumpscareController!
        if (jumpscareController != null)
        {
            Debug.Log("<color=red><b>MONSTER: INITIATING FATAL STRIKE SEQUENCE!</b></color>");
            jumpscareController.ExecuteJumpscare(activeStrikeType, monsterTransform, rampEntryTarget);
        }
        else
        {
            Debug.LogError("Missing JumpscareController reference!");
        }
    }

    public void StartEncounter()
    {
        if (isEncounterActive) return;
        isEncounterActive = true;
        TransitionToState(EncounterState.GracePeriod);
    }

    public void EndEncounter(bool playerWonMinigame)
    {
        if (!isEncounterActive || currentState == EncounterState.Strike || currentState == EncounterState.ClutchStruggle) return;

        if (playerWonMinigame)
        {
            TransitionToState(EncounterState.Retreat);
        }
    }
}