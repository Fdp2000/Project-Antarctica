using UnityEngine;

public class VehicleController : MonoBehaviour
{
    [Header("Vehicle Specs")]
    public float moveSpeed = 5.0f;
    public float turnSpeed = 45.0f;

    [Header("Visuals")]
    public Transform steeringWheel;
    [Tooltip("How far the wheel visually turns left/right")]
    public float maxSteeringAngle = 120.0f;
    private Quaternion initialSteeringRotation;

    [Header("Safety Dependencies")]
    public WinchController winch;
    public CRTWaveController scienceMachine;
    public SimpleFPSController player;

    [HideInInspector] public bool isPlayerDriving = false;

    void Start()
    {
        // Memorize the exact tilt and local rotation of the wheel when the game starts
        if (steeringWheel != null)
        {
            initialSteeringRotation = steeringWheel.localRotation;
        }
    }

    void Update()
    {
        if (isPlayerDriving)
        {
            // --- SAFETY CHECK SYSTEM ---
            bool doorOpen = (winch != null && !winch.IsDoorClosed);
            bool scienceActive = (scienceMachine != null && scienceMachine.enabled);
            bool cardWaiting = (player != null && player.IsPunchcardInTray());
            bool carryingTape = (player != null && player.hasCassette);

            if (doorOpen || scienceActive || cardWaiting || carryingTape)
            {
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

            // --- STEERING WHEEL ANIMATION ---
            if (steeringWheel != null)
            {
                // UPDATED: Shifted the math to the Y-axis!
                // *Note: If the wheel visually turns LEFT when you steer RIGHT, just change 'turnInput' to '-turnInput'
                Quaternion steeringTarget = initialSteeringRotation * Quaternion.Euler(0, turnInput * maxSteeringAngle, 0);

                // Smoothly apply the local rotation
                steeringWheel.localRotation = Quaternion.Lerp(steeringWheel.localRotation, steeringTarget, Time.deltaTime * 8f);
            }
        }
        else
        {
            // Smoothly return the wheel to dead-center when the player is not driving
            if (steeringWheel != null)
            {
                steeringWheel.localRotation = Quaternion.Lerp(steeringWheel.localRotation, initialSteeringRotation, Time.deltaTime * 8f);
            }
        }
    }
}