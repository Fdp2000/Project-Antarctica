using UnityEngine;

public class VehicleController : MonoBehaviour
{
    [Header("Vehicle Specs")]
    public float moveSpeed = 5.0f;
    public float turnSpeed = 45.0f;

    [Header("Safety Dependencies")]
    public WinchController winch;
    public CRTWaveController scienceMachine;
    public SimpleFPSController player;

    [HideInInspector] public bool isPlayerDriving = false;

    void Update()
    {
        if (isPlayerDriving)
        {
            // --- SAFETY CHECK SYSTEM ---

            // 1. Door Lock
            bool doorOpen = (winch != null && !winch.IsDoorClosed);

            // 2. Station Lock (The script disables itself when minigame is done)
            bool scienceActive = (scienceMachine != null && scienceMachine.enabled);

            // 3. Item Locks (Check if card is in tray or player is carrying a tape)
            bool cardWaiting = (player != null && player.IsPunchcardInTray());
            bool carryingTape = (player != null && player.hasCassette);

            if (doorOpen || scienceActive || cardWaiting || carryingTape)
            {
                // Display feedback in console to help with debugging
                if (doorOpen) Debug.LogWarning("Drive Locked: Rear door must be closed!");
                if (scienceActive) Debug.LogWarning("Drive Locked: Science station is active!");
                if (cardWaiting) Debug.LogWarning("Drive Locked: Collect the punchcard first!");
                if (carryingTape) Debug.LogWarning("Drive Locked: Store the cassette tape before driving!");

                return; // Prevent all movement inputs
            }

            // --- MOVEMENT INPUTS ---
            float moveInput = Input.GetAxis("Vertical");
            float turnInput = Input.GetAxis("Horizontal");

            transform.Translate(Vector3.forward * moveInput * moveSpeed * Time.deltaTime);
            transform.Rotate(Vector3.up * turnInput * turnSpeed * Time.deltaTime);
        }
    }
}