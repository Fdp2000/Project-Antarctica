using System;
using UnityEngine;

/// <summary>
/// Placed on the tape drive of the Science Machine. 
/// Receives the cassette from the player and broadcasts an event to the ScienceStationManager.
/// </summary>
[RequireComponent(typeof(Collider), typeof(Outline))]
public class CassetteReceiver : MonoBehaviour
{
    [Tooltip("The visual model of the tape already inside the drive. Should start disabled in the Scene.")]
    public GameObject insertedCassetteVisual;

    [HideInInspector] 
    public bool hasCassette = false;

    // We use a public Action so the ScienceStationManager can listen to it cleanly
    public event Action OnCassetteInserted;

    void Start()
    {
        Outline outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }

    public void InsertCassette()
    {
        hasCassette = true;
        
        // Turn on the mesh inside the drive
        if (insertedCassetteVisual != null)
        {
            insertedCassetteVisual.SetActive(true);
        }

        Debug.Log("<color=yellow>Cassette Inserted into the Drive!</color>");
        
        // Broadcast the event to anyone listening (i.e. the ScienceStationManager)
        OnCassetteInserted?.Invoke();
    }
}
