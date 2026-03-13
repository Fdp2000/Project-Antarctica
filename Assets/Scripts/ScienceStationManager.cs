using UnityEngine;

public class ScienceStationManager : MonoBehaviour
{
    [Header("Dependencies")]
    public CassetteReceiver cassetteReceiver;
    public WinchController winchController;
    public CRTWaveController crtWaveController;

    private bool isMinigameActive = false;

    void Start()
    {
        if (crtWaveController != null)
        {
            crtWaveController.TurnOffMachine();
        }

        if (cassetteReceiver != null)
        {
            // Now listens for the new event signature that includes the beacon
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
        if (cassetteReceiver != null) cassetteReceiver.OnCassetteInserted -= HandleCassetteInserted;
        if (winchController != null)
        {
            winchController.OnDoorFullyOpened -= HandleDoorOpened;
            winchController.OnDoorFullyClosed -= HandleDoorClosed;
        }
    }

    // Receives the beacon from the event
    void HandleCassetteInserted(RadioBeacon insertedBeacon)
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
        if (isMinigameActive) return;

        bool hasTape = cassetteReceiver != null && cassetteReceiver.hasCassette;
        bool doorOpen = winchController != null && winchController.IsDoorOpen;

        if (hasTape && doorOpen)
        {
            Debug.Log("<color=green>CONDITIONS MET: Booting Science Station!</color>");
            isMinigameActive = true;
            if (crtWaveController != null)
            {
                // INJECT THE BEACON INTO THE MINIGAME
                crtWaveController.TurnOnMachine(cassetteReceiver.currentlyInsertedBeacon);
            }
        }
    }
}