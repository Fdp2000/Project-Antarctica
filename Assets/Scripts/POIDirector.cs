using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class POITier
{
    [Tooltip("The difficulty index at which these POIs turn on.")]
    public int unlockAtDifficultyIndex;

    [Tooltip("The actual POI parent GameObjects (containing the models and the Radio Beacon).")]
    public GameObject[] poisToEnable;
}

public class POIDirector : MonoBehaviour
{
    public static POIDirector Instance;

    [Header("World Progression")]
    [Tooltip("Define which POIs unlock at which difficulty index.")]
    public List<POITier> progressionTiers = new List<POITier>();

    [Header("Debug")]
    [Tooltip("If checked, all POIs will be enabled immediately, ignoring the difficulty progression.")]
    public bool debugForceUnlockAll = false; // <--- NEW

    void Awake()
    {
        // Standard Singleton setup
        if (Instance == null)
        {
            Instance = this;
            EvaluatePOIs(0); // Evaluate when the game starts
        }
        else
        {
            Destroy(gameObject); // Destroy duplicates
        }
    }

    /// <summary>
    /// Evaluates all POIs. If the current difficulty meets the tier requirement, 
    /// the POI is enabled. If not, it remains completely hidden and deactivated.
    /// </summary>
    public void EvaluatePOIs(int currentDifficultyIndex)
    {
        Debug.Log($"<color=orange>[POI Director] Evaluating World State for Difficulty Index: {currentDifficultyIndex}</color>");

        foreach (var tier in progressionTiers)
        {
            bool isUnlocked = debugForceUnlockAll || (currentDifficultyIndex >= tier.unlockAtDifficultyIndex);

            foreach (var poi in tier.poisToEnable)
            {
                // CHANGE: Only proceed if the POI should be unlocked AND it isn't already active.
                // We no longer pass "false" to SetActive.
                if (poi != null && isUnlocked && !poi.activeSelf)
                {
                    poi.SetActive(true); // Hardcode this to true so it can NEVER disable anything
                    Debug.Log($"<color=yellow>   -> Unlocked POI: {poi.name}</color>");
                }
            }
        }
    }
}