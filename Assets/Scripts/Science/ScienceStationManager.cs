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
            cassetteReceiver.OnCassetteInserted += HandleCassetteInserted;
        }

        if (winchController != null)
        {
            winchController.OnDoorFullyOpened += HandleDoorOpened;
            // Changed this line to listen for the door *starting* to close
            winchController.OnDoorStartedClosing += HandleDoorStartedClosing;
        }
    }

    private void OnDestroy()
    {
        if (cassetteReceiver != null) cassetteReceiver.OnCassetteInserted -= HandleCassetteInserted;
        if (winchController != null)
        {
            winchController.OnDoorFullyOpened -= HandleDoorOpened;
            winchController.OnDoorStartedClosing -= HandleDoorStartedClosing;
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

    void HandleDoorStartedClosing()
    {
        if (isMinigameActive)
        {
            Debug.Log("<color=yellow>DOOR LEFT OPEN STATE: Suspending Science Station!</color>");
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