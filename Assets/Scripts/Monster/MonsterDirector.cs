using UnityEngine;

public class MonsterDirector : MonoBehaviour
{
    public enum EncounterState { Idle, GracePeriod, Approach, Silence, Strike, Retreat }

    [Header("Settings")]
    public DifficultyProfile currentDifficulty;

    [Header("Live Debug (Watch these in Play Mode)")]
    public EncounterState currentState = EncounterState.Idle;
    public float stateTimer = 0f;
    public bool isEncounterActive = false;

    void Update()
    {
        if (!isEncounterActive) return;

        // Tick down the timer for the current state
        if (stateTimer > 0)
        {
            stateTimer -= Time.deltaTime;
        }

        // State Machine Logic
        switch (currentState)
        {
            case EncounterState.GracePeriod:
                if (stateTimer <= 0) TransitionToState(EncounterState.Approach);
                break;

            case EncounterState.Approach:
                // TODO later: Lerp radio static volume up based on stateTimer percentage
                if (stateTimer <= 0) TransitionToState(EncounterState.Silence);
                break;

            case EncounterState.Silence:
                if (stateTimer <= 0) TransitionToState(EncounterState.Strike);
                break;

            case EncounterState.Strike:
                // If we reach this state, the player failed to close the door in time.
                TriggerJumpscare();
                break;

            case EncounterState.Retreat:
                // TODO later: Lerp radio static back to normal, run Siege Events
                if (stateTimer <= 0) TransitionToState(EncounterState.Idle);
                break;
        }
    }

    private void TransitionToState(EncounterState newState)
    {
        currentState = newState;

        if (currentDifficulty == null)
        {
            Debug.LogError("Monster Director is missing a Difficulty Profile!");
            return;
        }

        switch (newState)
        {
            case EncounterState.Idle:
                isEncounterActive = false;
                Debug.Log("<color=grey>MONSTER: Idle. Waiting in the fog.</color>");
                break;

            case EncounterState.GracePeriod:
                stateTimer = currentDifficulty.GetRandomizedTimer(currentDifficulty.baseGracePeriod);
                Debug.Log($"<color=white>MONSTER: Spawned. Grace period started for {stateTimer:F1}s.</color>");
                break;

            case EncounterState.Approach:
                stateTimer = currentDifficulty.GetRandomizedTimer(currentDifficulty.approachDuration);
                Debug.Log($"<color=yellow>MONSTER: Approaching! Radio static rising for {stateTimer:F1}s.</color>");
                break;

            case EncounterState.Silence:
                stateTimer = currentDifficulty.GetRandomizedTimer(currentDifficulty.silenceDuration);
                Debug.Log($"<color=orange>MONSTER: Threshold reached! Dead silence for {stateTimer:F1}s.</color>");
                break;

            case EncounterState.Retreat:
                stateTimer = currentDifficulty.GetRandomizedTimer(currentDifficulty.retreatDuration);
                Debug.Log($"<color=cyan>MONSTER: Retreating. Normalizing audio for {stateTimer:F1}s.</color>");
                break;
        }
    }

    // --- PUBLIC HOOKS FOR OTHER SCRIPTS ---

    public void StartEncounter()
    {
        if (isEncounterActive) return;
        isEncounterActive = true;
        TransitionToState(EncounterState.GracePeriod);
    }

    public void EndEncounter(bool playerWonMinigame)
    {
        if (!isEncounterActive || currentState == EncounterState.Strike) return;

        if (playerWonMinigame)
        {
            Debug.Log("<color=green>MONSTER: Player finished science! Monster is leaving.</color>");
        }
        else
        {
            Debug.Log("<color=blue>MONSTER: Door slammed shut! Monster locked out.</color>");
        }

        TransitionToState(EncounterState.Retreat);
    }

    private void TriggerJumpscare()
    {
        isEncounterActive = false; // Stop the loop
        Debug.Log("<color=red><b>MONSTER: STRIKE! PLAYER IS DEAD!</b></color>");
        // TODO: Freeze player, spawn monster, play animation, cut to black.
    }
}