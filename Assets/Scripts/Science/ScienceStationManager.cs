using UnityEngine;

public class ScienceStationManager : MonoBehaviour
{
    [Header("Dependencies")]
    public CassetteReceiver cassetteReceiver;
    public WinchController winchController;
    public CRTWaveController crtWaveController;

    [Header("Green Light (Active)")]
    public MeshRenderer greenBulbRenderer;
    public Light greenPointLight;
    public Material greenOnMaterial;
    public Material greenOffMaterial;

    [Header("Red Light (Alert)")]
    public MeshRenderer redBulbRenderer;
    public Light redPointLight;
    public Material redOnMaterial;
    public Material redOffMaterial;

    private bool isMinigameActive = false;

    [Header("Monster System")]
    public MonsterDirector monsterDirector;

    void Start()
    {
        SetLightState(greenBulbRenderer, greenPointLight, greenOffMaterial, false);
        SetLightState(redBulbRenderer, redPointLight, redOffMaterial, false);

        // Pass 'false' so it doesn't play a sound when the game starts
        if (crtWaveController != null) crtWaveController.TurnOffMachine(false);

        if (cassetteReceiver != null)
            cassetteReceiver.OnCassetteInserted += HandleCassetteInserted;

        if (winchController != null)
        {
            winchController.OnDoorFullyOpened += HandleDoorOpened;
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

    void HandleCassetteInserted(RadioBeacon insertedBeacon) => TryBootMachine();
    void HandleDoorOpened() => TryBootMachine();

    void HandleDoorStartedClosing()
    {
        if (crtWaveController != null && crtWaveController.isMinigameComplete)
        {
            if (cassetteReceiver != null && cassetteReceiver.currentlyInsertedBeacon == crtWaveController.linkedBeacon)
                return;
        }

        bool hasTape = cassetteReceiver != null && cassetteReceiver.hasCassette;

        if (isMinigameActive || hasTape)
        {
            Debug.Log("<color=red>SIGNAL BLOCKED: Door closed while tape is present!</color>");

            if (isMinigameActive && crtWaveController != null)
            {
                crtWaveController.ApplyInterruptionPenalty();
            }

            isMinigameActive = false;

            // Pass 'true' because we actively want to hear it fail and turn off!
            if (crtWaveController != null) crtWaveController.TurnOffMachine(true);

            SetLightState(greenBulbRenderer, greenPointLight, greenOffMaterial, false);
            SetLightState(redBulbRenderer, redPointLight, redOnMaterial, true);
        }
    }
    void TryBootMachine()
    {
        if (isMinigameActive) return;

        if (crtWaveController != null && crtWaveController.isMinigameComplete)
        {
            if (cassetteReceiver != null && cassetteReceiver.currentlyInsertedBeacon == crtWaveController.linkedBeacon)
                return;
        }

        bool hasTape = cassetteReceiver != null && cassetteReceiver.hasCassette;
        bool doorOpen = winchController != null && winchController.IsDoorOpen;

        if (hasTape && doorOpen)
        {
            Debug.Log("<color=green>SIGNAL ACQUIRED: Booting Science Station.</color>");
            isMinigameActive = true;

            if (monsterDirector != null) monsterDirector.StartEncounter();

            // --- THE FIX: Pass the difficulty profile to the CRT Controller! ---
            if (crtWaveController != null)
                crtWaveController.TurnOnMachine(cassetteReceiver.currentlyInsertedBeacon, monsterDirector.currentDifficulty);

            SetLightState(greenBulbRenderer, greenPointLight, greenOnMaterial, true);
            SetLightState(redBulbRenderer, redPointLight, redOffMaterial, false);
        }
        else if (hasTape && !doorOpen)
        {
            Debug.Log("<color=red>SIGNAL BLOCKED: Tape inserted but door is shut!</color>");
            SetLightState(greenBulbRenderer, greenPointLight, greenOffMaterial, false);
            SetLightState(redBulbRenderer, redPointLight, redOnMaterial, true);
        }
    }
    void Update()
    {
        if (isMinigameActive && crtWaveController != null && crtWaveController.isMinigameComplete)
        {
            isMinigameActive = false;
            if (monsterDirector != null) monsterDirector.EndEncounter(true);
            SetLightState(greenBulbRenderer, greenPointLight, greenOffMaterial, false);
            SetLightState(redBulbRenderer, redPointLight, redOffMaterial, false);
        }
    }

    private void SetLightState(MeshRenderer renderer, Light pLight, Material mat, bool isOn)
    {
        if (renderer != null && mat != null) renderer.material = mat;
        if (pLight != null) pLight.enabled = isOn;
    }
    // --- NEW: Resets the station completely if the player dies ---
public void ResetStation()
    {
        isMinigameActive = false;

        // Pass 'false' so it doesn't play a sound during the death/respawn fade
        if (crtWaveController != null) crtWaveController.TurnOffMachine(false); 
        if (cassetteReceiver != null) cassetteReceiver.ConsumeTape(); 

        SetLightState(greenBulbRenderer, greenPointLight, greenOffMaterial, false);
        SetLightState(redBulbRenderer, redPointLight, redOffMaterial, false);

        if (winchController != null && winchController.IsDoorOpen)
        {
            winchController.ForceSlamShut();
        }

        Debug.Log("<color=cyan>SCIENCE STATION RESET FOR NEXT ATTEMPT.</color>");
    }
}