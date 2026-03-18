using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomCenterOfMass : MonoBehaviour
{
    [Tooltip("Drag an empty GameObject here to act as the center of mass.")]
    public Transform centerOfMassTarget;

    void Start()
    {
        if (centerOfMassTarget != null)
        {
            // Sets the Rigidbody's center of mass to the position of your target
            GetComponent<Rigidbody>().centerOfMass = centerOfMassTarget.localPosition;
        }
        else
        {
            Debug.LogWarning("No Center of Mass target assigned to " + gameObject.name);
        }
    }
}