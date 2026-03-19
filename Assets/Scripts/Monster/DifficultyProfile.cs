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

    public float GetRandomizedTimer(float baseTime)
    {
        return Mathf.Max(0.1f, baseTime + Random.Range(-phaseVariance, phaseVariance));
    }
}