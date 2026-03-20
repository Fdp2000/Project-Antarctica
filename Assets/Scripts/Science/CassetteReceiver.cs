using System;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Outline))]
public class CassetteReceiver : MonoBehaviour
{
    public GameObject insertedCassetteVisual;
    [HideInInspector] public bool hasCassette = false;

    [HideInInspector] public RadioBeacon currentlyInsertedBeacon;
    public event Action<RadioBeacon> OnCassetteInserted;

    void Start()
    {
        Outline outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }

    public void InsertCassette(RadioBeacon beaconFromTape)
    {
        hasCassette = true;
        currentlyInsertedBeacon = beaconFromTape;

        if (insertedCassetteVisual != null) insertedCassetteVisual.SetActive(true);

        Debug.Log($"<color=yellow>Cassette Inserted! Source POI: {(beaconFromTape != null ? beaconFromTape.gameObject.name : "UNKNOWN")}</color>");
        OnCassetteInserted?.Invoke(currentlyInsertedBeacon);

        // --- NEW: Trigger the Save Checkpoint! ---
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveCheckpoint(beaconFromTape);
        }
    }

    // --- NEW: Resets the machine when the punchcard is collected ---
    public void ConsumeTape()
    {
        hasCassette = false;
        currentlyInsertedBeacon = null;

        if (insertedCassetteVisual != null) insertedCassetteVisual.SetActive(false);
        Debug.Log("<color=yellow>Tape Consumed! Machine ready for next POI.</color>");
    }
}