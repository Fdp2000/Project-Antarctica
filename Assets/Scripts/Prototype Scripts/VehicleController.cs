using UnityEngine;

public class VehicleController : MonoBehaviour
{
    [Header("Vehicle Specs")]
    public float moveSpeed = 5.0f; // Keep it low for that heavy feel
    public float turnSpeed = 45.0f; // Slow, clunky turning

    // The player script will turn this on/off when you press 'E'
    public bool isPlayerDriving = false;

    void Update()
    {
        // Only allow movement if the player is actually sitting in the seat
        if (isPlayerDriving)
        {
            float moveInput = Input.GetAxis("Vertical");   // W/S keys
            float turnInput = Input.GetAxis("Horizontal"); // A/D keys

            // 1. Move the vehicle forward/backward (Both tracks moving together)
            transform.Translate(Vector3.forward * moveInput * moveSpeed * Time.deltaTime);

            // 2. Tank Steering (Neutral Steer)
            // We removed the lock! You can now turn in place even if moveInput is 0.
            transform.Rotate(Vector3.up * turnInput * turnSpeed * Time.deltaTime);
        }
    }
}