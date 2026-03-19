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

        if (crtWaveController != null) crtWaveController.TurnOffMachine();

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
        if (crtWaveController != null && crtWaveController.isMinigameComplete) return;

        bool hasTape = cassetteReceiver != null && cassetteReceiver.hasCassette;

        if (isMinigameActive || hasTape)
        {
            Debug.Log("<color=red>SIGNAL BLOCKED: Door closed while tape is present!</color>");

            // --- THE PENALTY TRIGGER ---
            if (isMinigameActive && crtWaveController != null)
            {
                crtWaveController.ApplyInterruptionPenalty();
            }

            isMinigameActive = false;
            if (crtWaveController != null) crtWaveController.TurnOffMachine();

            // Switch to Alert State immediately
            SetLightState(greenBulbRenderer, greenPointLight, greenOffMaterial, false);
            SetLightState(redBulbRenderer, redPointLight, redOnMaterial, true);
        }
    }

    void TryBootMachine()
    {
        if (isMinigameActive) return;

        if (crtWaveController != null && crtWaveController.isMinigameComplete) return;

        bool hasTape = cassetteReceiver != null && cassetteReceiver.hasCassette;
        bool doorOpen = winchController != null && winchController.IsDoorOpen;

        if (hasTape && doorOpen)
        {
            Debug.Log("<color=green>SIGNAL ACQUIRED: Booting Science Station.</color>");
            isMinigameActive = true;
            if (monsterDirector != null) monsterDirector.StartEncounter();
            if (crtWaveController != null)
                crtWaveController.TurnOnMachine(cassetteReceiver.currentlyInsertedBeacon);

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
}