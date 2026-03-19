using UnityEngine;

public class ClutchController : MonoBehaviour
{
    [Header("Clutch Tuning (Base Mechanics)")]
    [Tooltip("The angle where the clutch struggle is triggered. (Matches MonsterDirector)")]
    public float clutchCutoffAngle = -133.3f;
    [Tooltip("The angle where the door is fully closed.")]
    public float closedAngle = -90f;

    [Header("Clutch Tuning (RNG Modifiers)")]
    [Tooltip("Minimum penalty/bonus to the player's reaction score. (Negative = Penalty)")]
    public float minAdrenalineFumble = -15f;
    [Tooltip("Maximum penalty/bonus to the player's reaction score. (Positive = Lucky save)")]
    public float maxAdrenalineFumble = 5f;

    [Tooltip("Minimum multiplier applied to the monster's strength. (< 1.0 = Weaker)")]
    public float minMonsterSurge = 0.8f;
    [Tooltip("Maximum multiplier applied to the monster's strength. (> 1.0 = Overpowering)")]
    public float maxMonsterSurge = 1.3f;

    [Header("Live Debug (Last Struggle)")]
    public float lastReactionScore;
    public float lastAngleScore;
    public float lastPlayerTotal;
    public float lastMonsterTotal;
    public bool lastResultWon;

    // Called by the MonsterDirector the exact millisecond the Strike timer hits 0
    public void EvaluateStruggle(float playerReactionTime, float maxDangerTime, float currentDoorAngle, DifficultyProfile difficulty)
    {
        Debug.Log("<color=cyan>--- CLUTCH STRUGGLE INITIATED ---</color>");

        // 1. THE REACTION SCORE
        float baseReactionScore = 0f;
        if (playerReactionTime >= 0f)
        {
            // The faster they reacted, the closer to 100 they get.
            baseReactionScore = Mathf.Lerp(100f, 0f, (playerReactionTime / maxDangerTime));
        }
        else
        {
            // They never touched the door during the Danger Zone!
            Debug.Log("<color=red>Player never grabbed the door! Base Reaction Score: 0</color>");
        }

        // Apply the exposed "Adrenaline Fumble" 
        float adrenalineFumble = Random.Range(minAdrenalineFumble, maxAdrenalineFumble);
        lastReactionScore = Mathf.Clamp(baseReactionScore + adrenalineFumble, 0f, 100f);

        // 2. THE ANGLE SCORE
        // Maps the door's position to a 0-100 score. Closer to -90 = higher score.
        lastAngleScore = Mathf.InverseLerp(clutchCutoffAngle, closedAngle, currentDoorAngle) * 100f;

        // 3. THE PLAYER TOTAL
        lastPlayerTotal = lastReactionScore + lastAngleScore;

        // 4. THE MONSTER SURGE
        float baseRoll = difficulty.baseMonsterClutchStrength + Random.Range(-difficulty.clutchStrengthVariance, difficulty.clutchStrengthVariance);

        // Apply the exposed monster multiplier
        float surgeMultiplier = Random.Range(minMonsterSurge, maxMonsterSurge);
        lastMonsterTotal = baseRoll * surgeMultiplier;

        // 5. THE RESULT
        lastResultWon = lastPlayerTotal >= lastMonsterTotal;

        // --- DETAILED LOGGING FOR BALANCING ---
        Debug.Log($"Door Angle: {currentDoorAngle:F1}° | Angle Score: {lastAngleScore:F1}/100");
        Debug.Log($"Reaction Time: {playerReactionTime:F2}s / {maxDangerTime:F2}s | Reaction Score (w/ Fumble): {lastReactionScore:F1}/100");
        Debug.Log($"<color=white><b>PLAYER TOTAL POWER: {lastPlayerTotal:F1}</b></color>");
        Debug.Log($"<color=orange><b>MONSTER TOTAL POWER: {lastMonsterTotal:F1} (Surge: {surgeMultiplier:F2}x)</b></color>");

        if (lastResultWon)
        {
            Debug.Log("<color=green><b>[CLUTCH CALCULATED: PLAYER WINS!]</b> The player overpowered the monster!</color>");
            // TODO (Stage 2): Trigger Winch ForceSlamShut() & Monster Siege State
        }
        else
        {
            Debug.Log("<color=red><b>[CLUTCH CALCULATED: PLAYER LOSES!]</b> The monster broke through!</color>");
            // TODO (Stage 2): Trigger the 3-Strike Struggle / Jumpscare Router
        }

        Debug.Log("<color=cyan>---------------------------------</color>");
    }
}