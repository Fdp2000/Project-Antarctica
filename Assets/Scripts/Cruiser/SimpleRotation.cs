using UnityEngine;

public class SimpleRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Speed of rotation in degrees per second.")]
    public float rotationSpeed = 90f;

    [Tooltip("The axis to rotate around (usually Up for an anemometer).")]
    public Vector3 rotationAxis = Vector3.up;

    void Update()
    {
        // Rotates the object every frame based on time
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}