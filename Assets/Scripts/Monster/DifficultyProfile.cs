using UnityEngine;

[CreateAssetMenu(fileName = "New Difficulty Profile", menuName = "Antarctica/Difficulty Profile")]
public class DifficultyProfile : ScriptableObject
{
    [Header("Encounter Timers (Seconds)")]
    public float baseGracePeriod = 15f;
    public float approachDuration = 10f;
    public float silenceDuration = 3f;
    public float retreatDuration = 8f;

    [Header("Randomization")]
    [Tooltip("How much random time (in seconds) is added or subtracted from the timers to keep it unpredictable.")]
    public float phaseVariance = 2f;

    [Header("Siege Settings")]
    public float patienceThreshold = 10f;
    public float minSiegeEventInterval = 2f;
    public float maxSiegeEventInterval = 6f;

    // Helper method to get a randomized timer based on the variance
    public float GetRandomizedTimer(float baseTime)
    {
        return Mathf.Max(0.1f, baseTime + Random.Range(-phaseVariance, phaseVariance));
    }
}