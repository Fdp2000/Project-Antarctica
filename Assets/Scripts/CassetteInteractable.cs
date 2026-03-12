using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Outline))]
public class CassetteInteractable : MonoBehaviour
{
    [Tooltip("The POI this tape belongs to. Will automatically find it if placed as a child.")]
    public RadioBeacon sourceBeacon;

    void Start()
    {
        Outline outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        // Auto-assign: If you forgot to drag the beacon in the inspector, 
        // it searches its parent objects to find it!
        if (sourceBeacon == null)
        {
            sourceBeacon = GetComponentInParent<RadioBeacon>();

            if (sourceBeacon == null)
            {
                Debug.LogWarning("Cassette tape spawned without a parent Radio Beacon!");
            }
        }
    }
}