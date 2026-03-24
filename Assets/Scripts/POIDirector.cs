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

    void Awake()
    {
        // Standard Singleton setup
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
            // Check if the player has reached or passed this tier's required difficulty
            bool isUnlocked = currentDifficultyIndex >= tier.unlockAtDifficultyIndex;

            foreach (var poi in tier.poisToEnable)
            {
                if (poi != null && poi.activeSelf != isUnlocked)
                {
                    poi.SetActive(isUnlocked);
                    Debug.Log($"<color=yellow>   -> {(isUnlocked ? "Unlocked" : "Locked")} POI: {poi.name}</color>");
                }
            }
        }
    }
}