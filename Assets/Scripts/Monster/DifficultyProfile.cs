using UnityEngine;

[CreateAssetMenu(fileName = "New Difficulty Profile", menuName = "Antarctica/Difficulty Profile")]
public class DifficultyProfile : ScriptableObject
{
    [Header("Encounter Timers (Seconds)")]
    public float baseGracePeriod = 15f;
    public float approachDuration = 10f;
    public float silenceDuration = 3f;

    [Tooltip("How long it takes the monster to cross the fog and hit the ramp during the Strike.")]
    public float strikeDuration = 1.0f;

    public float retreatDuration = 8f;

    [Header("Randomization")]
    [Tooltip("How much random time (in seconds) is added or subtracted from the timers to keep it unpredictable.")]
    public float phaseVariance = 2f;

    [Header("Siege Settings")]
    public float patienceThreshold = 10f;
    public float minSiegeEventInterval = 2f;
    public float maxSiegeEventInterval = 6f;

    [Header("Clutch Settings (Tug-of-War)")]
    [Tooltip("The base strength the monster uses against the player's math score.")]
    public float baseMonsterClutchStrength = 120f;
    [Tooltip("Random variance added or subtracted to the monster's strength each encounter.")]
    public float clutchStrengthVariance = 20f;

    // --- NEW: Science Minigame Settings ---
    [Header("Science Minigame (Target Wave)")]
    public float baseTargetAmplitude = 0.7f;
    public float baseTargetFrequency = 8.0f;
    public float baseTargetPhase = 10.43f;

    [Header("Science Minigame (Target Drift)")]
    public float minDriftInterval = 4.0f;
    public float maxDriftInterval = 6.0f;
    public float driftLerpDuration = 3.0f;

    public float amplitudeDriftVariance = 1.3f;
    public float frequencyDriftVariance = 1.5f;
    public float phaseDriftVariance = 1.5f;

    public float GetRandomizedTimer(float baseTime)
    {
        return Mathf.Max(0.1f, baseTime + Random.Range(-phaseVariance, phaseVariance));
    }
}