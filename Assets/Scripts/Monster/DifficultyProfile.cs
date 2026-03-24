using UnityEngine;

[CreateAssetMenu(fileName = "New Difficulty Profile", menuName = "Antarctica/Difficulty Profile")]
public class DifficultyProfile : ScriptableObject
{
    [Header("Encounter Timers (X = Min, Y = Max Seconds)")]
    public Vector2 gracePeriod = new Vector2(10f, 20f);
    public Vector2 approachDuration = new Vector2(8f, 12f);
    public Vector2 silenceDuration = new Vector2(2f, 4f);

    [Tooltip("How long it takes the monster to cross the fog during the Strike.")]
    public Vector2 strikeDuration = new Vector2(1.0f, 1.0f); // Usually keep these tight/identical

    public Vector2 retreatDuration = new Vector2(6f, 10f);

    [Header("Siege Settings (X = Min, Y = Max)")]
    public Vector2 patienceThreshold = new Vector2(8f, 12f);

    [Tooltip("Note: These were already separate min/max, grouped them into Vector2 for consistency!")]
    public Vector2 siegeEventInterval = new Vector2(2f, 6f);

    [Header("Clutch Settings (Tug-of-War)")]
    public float baseMonsterClutchStrength = 120f;
    public float clutchStrengthVariance = 20f;

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

    // --- NEW RANDOMIZER ---
    public float GetRandomTimer(Vector2 minMax)
    {
        return Mathf.Max(0.1f, Random.Range(minMax.x, minMax.y));
    }
}