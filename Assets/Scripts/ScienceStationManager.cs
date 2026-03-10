using UnityEngine;

/// <summary>
/// This is the brain of the minigame start sequence. 
/// It waits for the cassette to be inserted AND the door to be fully open before booting the CRT.
/// </summary>
public class ScienceStationManager : MonoBehaviour
{
    [Header("Dependencies")]
    public CassetteReceiver cassetteReceiver;
    public WinchController winchController;
    public CRTWaveController crtWaveController;

    private bool isMinigameActive = false;

    void Start()
    {
        // Make sure the CRT starts OFF. The wave controller's Start method will do its own initializations, 
        // so we can safely tell it to sleep.
        if (crtWaveController != null)
        {
            crtWaveController.TurnOffMachine();
        }

        // Subscribe to our hardware events
        if (cassetteReceiver != null)
        {
            cassetteReceiver.OnCassetteInserted += HandleCassetteInserted;
        }

        if (winchController != null)
        {
            winchController.OnDoorFullyOpened += HandleDoorOpened;
            winchController.OnDoorFullyClosed += HandleDoorClosed;
        }
    }

    private void OnDestroy()
    {
        // Always unsubscribe from events when destroyed to prevent memory leaks
        if (cassetteReceiver != null)
        {
            cassetteReceiver.OnCassetteInserted -= HandleCassetteInserted;
        }
        if (winchController != null)
        {
            winchController.OnDoorFullyOpened -= HandleDoorOpened;
            winchController.OnDoorFullyClosed -= HandleDoorClosed;
        }
    }

    void HandleCassetteInserted()
    {
        TryBootMachine();
    }

    void HandleDoorOpened()
    {
        TryBootMachine();
    }

    void HandleDoorClosed()
    {
        if (isMinigameActive)
        {
            Debug.Log("<color=yellow>DOOR CLOSED: Suspending Science Station!</color>");
            isMinigameActive = false;
            
            if (crtWaveController != null)
            {
                crtWaveController.TurnOffMachine();
            }
        }
    }

    void TryBootMachine()
    {
        Debug.Log("--- Science Station Boot Check ---");
        
        // Don't boot if we are already running
        if (isMinigameActive)
        {
            Debug.Log("Aborting: Minigame is already active.");
            return;
        }

        // Check if our references are assigned!
        if (cassetteReceiver == null) Debug.LogWarning("ScienceStationManager: CassetteReceiver is not assigned in Inspector!");
        if (winchController == null) Debug.LogWarning("ScienceStationManager: WinchController is not assigned in Inspector!");
        if (crtWaveController == null) Debug.LogWarning("ScienceStationManager: CRTWaveController is not assigned in Inspector!");

        bool hasTape = cassetteReceiver != null && cassetteReceiver.hasCassette;
        bool doorOpen = winchController != null && winchController.IsDoorOpen;

        Debug.Log($"Status -> Has Tape: {hasTape} | Is Door Open: {doorOpen}");

        // We need both the tape, and an open door
        if (hasTape && doorOpen)
        {
            Debug.Log("<color=green>CONDITIONS MET: Booting Science Station!</color>");
            isMinigameActive = true;
            if (crtWaveController != null)
            {
                crtWaveController.TurnOnMachine();
            }
        }
        else
        {
            Debug.Log("<color=red>CONDITIONS NOT MET.</color>");
        }
    }
}
