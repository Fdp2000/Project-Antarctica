using UnityEngine;

/// <summary>
/// A flag component for the Cassette pickup. 
/// Placed on the physical tape object in the world. 
/// The SimpleFPSController handles the actual pickup and destruction logic when it sees this.
/// </summary>
[RequireComponent(typeof(Collider), typeof(Outline))]
public class CassetteInteractable : MonoBehaviour
{
    void Start()
    {
        Outline outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }
}
