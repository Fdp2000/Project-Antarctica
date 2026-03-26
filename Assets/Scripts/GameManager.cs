using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // A Singleton so other scripts can easily call GameManager.Instance
    public static GameManager Instance;

    [Header("UI & Polish")]
    [Tooltip("Drag a full-screen black UI Image here.")]
    public Image blackScreen;
    [Tooltip("How long the screen stays pitch black before fading.")]
    public float blackScreenDuration = 2.0f;
    [Tooltip("How long the fade-in takes.")]
    public float fadeDuration = 1.5f;
    public float delayBeforeBlackScreen = 0.5f; // <--- NEW

    [Header("Dependencies")]
    public SimpleFPSController player;
    public Transform vehicle;
    public MonsterDirector monsterDirector;

    [Tooltip("Drag ALL of your RadioBeacon POIs in the game here so the save system can look them up by name.")]
    public RadioBeacon[] allBeaconsInGame;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (blackScreen != null)
        {
            // Ensure the screen starts completely clear and doesn't block clicks
            blackScreen.color = new Color(0, 0, 0, 0);
            blackScreen.raycastTarget = false;
        }
    }

    // --- THE SAVE SYSTEM (Will be hooked up in Step 2) ---
    public void SaveCheckpoint(RadioBeacon insertedBeacon)
    {
        Debug.Log("<color=cyan>GAME SAVED: Writing to PlayerPrefs.</color>");

        // Save Vehicle Position
        if (vehicle != null)
        {
            PlayerPrefs.SetFloat("VehX", vehicle.position.x);
            PlayerPrefs.SetFloat("VehY", vehicle.position.y);
            PlayerPrefs.SetFloat("VehZ", vehicle.position.z);
            PlayerPrefs.SetFloat("VehRotY", vehicle.eulerAngles.y);
        }

        // Save Player Position (Offset)
        if (player != null)
        {
            PlayerPrefs.SetFloat("PlayerX", player.transform.position.x);
            PlayerPrefs.SetFloat("PlayerY", player.transform.position.y);
            PlayerPrefs.SetFloat("PlayerZ", player.transform.position.z);
            PlayerPrefs.SetFloat("PlayerRotY", player.transform.eulerAngles.y);
        }

        // Save Minigame State & Tape
        if (insertedBeacon != null) PlayerPrefs.SetString("SavedTape", insertedBeacon.gameObject.name);
        if (monsterDirector != null) PlayerPrefs.SetInt("SavedDifficulty", monsterDirector.currentProgressionIndex);

        PlayerPrefs.Save();
    }

    // --- THE DEATH & RESPAWN SEQUENCE ---
    public void TriggerPlayerDeath()
    {
        StartCoroutine(DeathAndRespawnRoutine());
    }

    private IEnumerator DeathAndRespawnRoutine()
    {
        // 1. Disable Controls completely so the player can't move while waiting
        if (player != null) player.enabled = false;

        // --- NEW: Wait before cutting to black ---
        if (delayBeforeBlackScreen > 0f)
        {
            yield return new WaitForSeconds(delayBeforeBlackScreen);
        }

        // 2. Instantly Cut to Black
        if (blackScreen != null)
        {
            blackScreen.color = Color.black;
            blackScreen.raycastTarget = true;
        }

        // 3. Wait in terrifying silence for a moment
        yield return new WaitForSeconds(blackScreenDuration);

        // 4. Execute the Load/Respawn Physics in the dark
        LoadCheckpoint();

        // 5. Give controls back EXACTLY as the fade begins
        if (player != null) player.enabled = true;

        // 6. Slowly Fade In
        if (blackScreen != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                blackScreen.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            blackScreen.raycastTarget = false;
        }
    }
    private void LoadCheckpoint()
    {
        Debug.Log("<color=cyan>LOADING CHECKPOINT...</color>");

        // Reset the cameras back to normal BEFORE fading in (Fixes the Camera Trap!)
        if (monsterDirector != null && monsterDirector.jumpscareController != null)
        {
            monsterDirector.jumpscareController.ResetJumpscareState();
        }

        // 1. Restore Difficulty & Reset Monster
        if (monsterDirector != null && PlayerPrefs.HasKey("SavedDifficulty"))
        {
            monsterDirector.currentProgressionIndex = PlayerPrefs.GetInt("SavedDifficulty");
            monsterDirector.currentDifficulty = monsterDirector.difficultyProgression[monsterDirector.currentProgressionIndex];
            monsterDirector.TransitionToState(MonsterDirector.EncounterState.Idle);

            // --- NEW: Revert the world to the correct POI tier ---
            if (POIDirector.Instance != null) POIDirector.Instance.EvaluatePOIs(monsterDirector.currentProgressionIndex);
        }

        // 2. Teleport Vehicle
        if (vehicle != null && PlayerPrefs.HasKey("VehX"))
        {
            Vector3 vehPos = new Vector3(PlayerPrefs.GetFloat("VehX"), PlayerPrefs.GetFloat("VehY"), PlayerPrefs.GetFloat("VehZ"));
            vehicle.position = vehPos;
            vehicle.rotation = Quaternion.Euler(0, PlayerPrefs.GetFloat("VehRotY"), 0);
        }

        // 3. Teleport Player
        if (player != null && PlayerPrefs.HasKey("PlayerX"))
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Vector3 playerPos = new Vector3(PlayerPrefs.GetFloat("PlayerX"), PlayerPrefs.GetFloat("PlayerY"), PlayerPrefs.GetFloat("PlayerZ"));
            player.transform.position = playerPos;
            player.transform.rotation = Quaternion.Euler(0, PlayerPrefs.GetFloat("PlayerRotY"), 0);

            player.playerCamera.transform.localRotation = Quaternion.Euler(0, 0, 0);

            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
        }

        // 4. Reset the Science Station Machine
        ScienceStationManager station = FindObjectOfType<ScienceStationManager>();
        if (station != null) station.ResetStation();

        // --- NEW: Reset the CRT Wave Minigame completely ---
        CRTWaveController crtController = FindObjectOfType<CRTWaveController>();
        if (crtController != null)
        {
            crtController.currentProgress = 0f;
            crtController.completionTimeExtension = 0f; // Clear any interruption penalties!
            crtController.isMinigameComplete = false;
            crtController.TurnOffMachine(false); // Force it to turn off silently so it isn't running when they respawn
        }

        // 5. Give the Tape Back to the Player
        if (PlayerPrefs.HasKey("SavedTape"))
        {
            string tapeName = PlayerPrefs.GetString("SavedTape");
            RadioBeacon foundBeacon = null;

            foreach (var beacon in allBeaconsInGame)
            {
                if (beacon != null && beacon.gameObject.name == tapeName)
                {
                    foundBeacon = beacon;
                    break;
                }
            }

            if (foundBeacon != null && player != null)
            {
                player.hasCassette = true;
                player.currentlyHeldTapeBeacon = foundBeacon;

                if (player.heldCassetteVisual != null)
                {
                    player.heldCassetteVisual.SetActive(true);

                    // --- THE DIRECT METHOD ---
                    MeshRenderer tapeRenderer = player.heldCassetteVisual.GetComponentInChildren<MeshRenderer>();
                    if (tapeRenderer != null && foundBeacon.uniqueTapeMaterial != null)
                    {
                        tapeRenderer.material = foundBeacon.uniqueTapeMaterial;
                    }
                }
                Debug.Log($"<color=cyan>Restored Tape to Player Hand: {tapeName}</color>");
            }
        }
    }
}