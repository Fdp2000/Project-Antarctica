using UnityEngine;
using System.Collections;
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
    public bool debugForceUnlockAll = false;

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

        // --- THE FIX: Start the stagger coroutine instead of doing it all instantly ---
        StartCoroutine(StaggeredEvaluatePOIs(currentDifficultyIndex));
    }

    // --- THE FIX: The new Coroutine that prevents the CPU freeze ---
    private IEnumerator StaggeredEvaluatePOIs(int currentDifficultyIndex)
    {
        foreach (var tier in progressionTiers)
        {
            bool isUnlocked = debugForceUnlockAll || (currentDifficultyIndex >= tier.unlockAtDifficultyIndex);

            foreach (var poi in tier.poisToEnable)
            {
                if (poi != null && isUnlocked && !poi.activeSelf)
                {
                    poi.SetActive(true);
                    Debug.Log($"<color=yellow>   -> Unlocked POI: {poi.name}</color>");

                    // Tell Unity to pause this script and wait for the next frame before turning on the next POI
                    yield return null;
                }
            }
        }
    }
}