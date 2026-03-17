using System;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Outline))]
public class CassetteReceiver : MonoBehaviour
{
    public GameObject insertedCassetteVisual;
    [HideInInspector] public bool hasCassette = false;

    // Memory bank for the currently inserted tape
    [HideInInspector] public RadioBeacon currentlyInsertedBeacon;

    // The event now passes the specific beacon data to anyone listening
    public event Action<RadioBeacon> OnCassetteInserted;

    void Start()
    {
        Outline outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }

    // Your SimpleFPSController calls this and passes the data
    public void InsertCassette(RadioBeacon beaconFromTape)
    {
        hasCassette = true;
        currentlyInsertedBeacon = beaconFromTape;

        if (insertedCassetteVisual != null)
        {
            insertedCassetteVisual.SetActive(true);
        }

        Debug.Log($"<color=yellow>Cassette Inserted! Source POI: {(beaconFromTape != null ? beaconFromTape.gameObject.name : "UNKNOWN")}</color>");

        // Broadcast the event WITH the specific beacon data
        OnCassetteInserted?.Invoke(currentlyInsertedBeacon);
    }
}