using System;
using UnityEngine;

/// <summary>
/// A stub version of the EncounterDirector. 
/// In the future, this will control narrative events and monster encounters.
/// Right now, it just broadcasts the OnRadioInterferenceStarted event for the S-Meter to react to.
/// </summary>
public class EncounterDirector : MonoBehaviour
{
    public static EncounterDirector Instance { get; private set; }

    // The event the S-Meter will listen for
    public event Action OnRadioInterferenceStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    [ContextMenu("DEBUG: Trigger Monster Jamming")]
    public void TriggerInterference()
    {
        Debug.Log("<color=red>ENCOUNTER DIRECTOR: Monster Interference Started!</color>");
        OnRadioInterferenceStarted?.Invoke();
    }
}
