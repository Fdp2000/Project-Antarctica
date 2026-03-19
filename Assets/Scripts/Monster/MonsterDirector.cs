using UnityEngine;

public class MonsterDirector : MonoBehaviour
{
    public enum EncounterState { Idle, GracePeriod, Approach, Silence, Strike, Siege, Retreat, ClutchStruggle }

    [Header("Dependencies")]
    public DifficultyProfile currentDifficulty;
    public WinchController winchController;

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
    public float stateTimer = 0f;
    public bool isEncounterActive = false;

    private float currentMaxApproachTime;
    private float currentSprintSpeed;
    private bool isShortSilenceMode = false;

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
                // Just wait invisibly!
                stateTimer -= Time.deltaTime;

                if (stateTimer <= 0)
                {
                    if (winchController != null && winchController.IsDoorClosed)
                        TransitionToState(EncounterState.Siege);
                    else if (winchController != null && winchController.CurrentAngle < clutchCutoffAngle)
                        TransitionToState(EncounterState.Strike); // Gap is wide open
                    else
                        TransitionToState(EncounterState.ClutchStruggle); // Gap is small
                }
                break;

            case EncounterState.Strike:
                // The physical sprint!
                stateTimer -= Time.deltaTime;

                if (monsterTransform != null && rampEntryTarget != null)
                {
                    Vector3 flatTarget = new Vector3(rampEntryTarget.position.x, monsterTransform.position.y, rampEntryTarget.position.z);
                    monsterTransform.position = Vector3.MoveTowards(monsterTransform.position, flatTarget, currentSprintSpeed * Time.deltaTime);
                }

                if (stateTimer <= 0)
                {
                    TriggerJumpscare();
                }
                break;

            case EncounterState.Siege:
                if (winchController != null && !winchController.IsDoorClosed)
                {
                    Debug.Log("<color=red>MONSTER: Player opened the door during Siege! FATAL PEEK!</color>");
                    isShortSilenceMode = true;
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

            case EncounterState.ClutchStruggle:
                // Pauses here for the minigame
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
                isShortSilenceMode = false;
                if (monsterTransform != null) monsterTransform.gameObject.SetActive(false);
                Debug.Log($"<color=white>MONSTER: Grace period active for {stateTimer:F1}s.</color>");
                break;

            case EncounterState.Approach:
                currentMaxApproachTime = currentDifficulty.GetRandomizedTimer(currentDifficulty.approachDuration);
                stateTimer = currentMaxApproachTime;
                Debug.Log($"<color=yellow>MONSTER: Approaching! Radio static rising (Max Time: {stateTimer:F1}s).</color>");
                break;

            case EncounterState.Silence:
                stateTimer = isShortSilenceMode ? currentDifficulty.GetRandomizedTimer(siegePeekSilenceDuration) : currentDifficulty.GetRandomizedTimer(currentDifficulty.silenceDuration);
                Debug.Log($"<color=orange>MONSTER: Dead silence for {stateTimer:F1}s.</color>");
                break;

            case EncounterState.Strike:
                stateTimer = currentDifficulty.strikeDuration;
                SpawnAndCalculateMonsterSprint();
                if (monsterTransform != null) monsterTransform.gameObject.SetActive(true);
                Debug.Log($"<color=red>MONSTER: SPOTTED! Sprinting at player for {stateTimer:F1}s!</color>");
                break;

            case EncounterState.ClutchStruggle:
                if (monsterTransform != null) monsterTransform.gameObject.SetActive(true);
                Debug.Log("<color=purple>MONSTER: CLUTCH MOMENT! Monster jamming hands in gap!</color>");
                break;

            case EncounterState.Siege:
                stateTimer = currentDifficulty.GetRandomizedTimer(currentDifficulty.patienceThreshold);
                Debug.Log($"<color=magenta>MONSTER: SIEGE PHASE! Pacing outside for {stateTimer:F1}s.</color>");
                break;

            case EncounterState.Retreat:
                stateTimer = currentDifficulty.GetRandomizedTimer(currentDifficulty.retreatDuration);
                Debug.Log($"<color=cyan>MONSTER: Retreating. Normalizing audio for {stateTimer:F1}s.</color>");
                break;
        }
    }

    private void SpawnAndCalculateMonsterSprint()
    {
        if (monsterTransform == null || rampEntryTarget == null) return;

        float randomAngle = Random.Range(spawnAngleClamp.x, spawnAngleClamp.y);
        Vector3 direction = Quaternion.Euler(0, randomAngle, 0) * rampEntryTarget.forward;
        direction.y = 0;

        monsterTransform.position = rampEntryTarget.position + (direction.normalized * spawnRadius);

        Vector3 lookTarget = new Vector3(rampEntryTarget.position.x, monsterTransform.position.y, rampEntryTarget.position.z);
        monsterTransform.LookAt(lookTarget);

        float flatDistance = Vector2.Distance(new Vector2(monsterTransform.position.x, monsterTransform.position.z), new Vector2(rampEntryTarget.position.x, rampEntryTarget.position.z));
        currentSprintSpeed = flatDistance / stateTimer;
    }

    private void TriggerJumpscare()
    {
        isEncounterActive = false;
        Debug.Log("<color=red><b>MONSTER: REACHED PLAYER! FATAL JUMPSCARE!</b></color>");
        // TODO: Cut to black, freeze controls.
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