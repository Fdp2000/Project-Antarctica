using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [Header("Vehicle Specs (Transform Movement)")]
    public float maxSpeed = 5.0f;
    public float acceleration = 2.0f;
    public float deceleration = 4.0f;
    public float turnSpeed = 45.0f;

    [Header("Obstacle Radar (BoxCast)")]
    [Tooltip("Center of the radar box relative to the vehicle")]
    public Vector3 radarOffset = new Vector3(0, 1f, 0); [Tooltip("Half-size of the radar box. Make this slightly larger than your vehicle's body")]
    public Vector3 radarHalfExtents = new Vector3(2f, 1.5f, 3f);
    [Tooltip("How far ahead to look before stopping")]
    public float stopBufferDistance = 0.5f;
    [Tooltip("Only hit colliders on these layers to save performance")]
    public LayerMask obstacleLayer = ~0;

    [Header("Visuals")]
    public Transform steeringWheel;
    public float maxSteeringAngle = 120.0f;
    private Quaternion initialSteeringRotation;

    [Header("Safety Dependencies")]
    public WinchController winch;
    public CRTWaveController scienceMachine;
    public SimpleFPSController player;

    [HideInInspector] public bool isPlayerDriving = false;

    // State Variables
    private Rigidbody rb;
    private float currentMoveInput = 0f;
    private float currentTurnInput = 0f;
    private bool isMovementLocked = false;
    private float currentSpeed = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Ensure Rigidbody is kinematic so we have zero physics jank
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

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

                isMovementLocked = true;
                currentMoveInput = 0f;
                currentTurnInput = 0f;
                return;
            }

            isMovementLocked = false;

            // --- GATHER INPUTS ---
            currentMoveInput = Input.GetAxisRaw("Vertical");
            currentTurnInput = Input.GetAxisRaw("Horizontal");

            // --- STEERING WHEEL ANIMATION ---
            if (steeringWheel != null)
            {
                Quaternion steeringTarget = initialSteeringRotation * Quaternion.Euler(0, currentTurnInput * maxSteeringAngle, 0);
                steeringWheel.localRotation = Quaternion.Lerp(steeringWheel.localRotation, steeringTarget, Time.deltaTime * 8f);
            }
        }
        else
        {
            isMovementLocked = true;
            currentMoveInput = 0f;
            currentTurnInput = 0f;

            if (steeringWheel != null)
            {
                steeringWheel.localRotation = Quaternion.Lerp(steeringWheel.localRotation, initialSteeringRotation, Time.deltaTime * 8f);
            }
        }
    }

    void FixedUpdate()
    {
        // 1. Calculate Target Speed (Smooth acceleration)
        float targetSpeed = isMovementLocked ? 0f : currentMoveInput * maxSpeed;

        if (Mathf.Abs(targetSpeed) > 0.1f)
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.fixedDeltaTime);

        // --- NEW: ROTATION RADAR CHECK ---
        float turnAmount = currentTurnInput * turnSpeed * Time.fixedDeltaTime;
        bool canRotate = true;

        if (Mathf.Abs(turnAmount) > 0.01f)
        {
            // Calculate what our rotation WOULD be if we turned
            Quaternion potentialRotation = rb.rotation * Quaternion.Euler(0, turnAmount, 0);
            Vector3 radarCenter = transform.position + potentialRotation * radarOffset;

            // Check if this new rotation would cause a collision
            if (Physics.CheckBox(radarCenter, radarHalfExtents, potentialRotation, obstacleLayer))
            {
                // We do a quick overlap check to see if we hit an inviswall
                Collider[] hits = Physics.OverlapBox(radarCenter, radarHalfExtents, potentialRotation, obstacleLayer);
                foreach (var hitCol in hits)
                {
                    if (hitCol.CompareTag("inviswall"))
                    {
                        canRotate = false; // Block the turn
                        break;
                    }
                }
            }
        }

        // Apply Rotation only if safe
        if (!isMovementLocked && canRotate)
        {
            Quaternion turnRotation = Quaternion.Euler(0, turnAmount, 0);
            rb.MoveRotation(rb.rotation * turnRotation);
        }

        // --- MOVEMENT RADAR CHECK ---
        if (Mathf.Abs(currentSpeed) > 0.01f)
        {
            Vector3 moveDirection = (currentSpeed > 0) ? transform.forward : -transform.forward;
            Vector3 radarCenter = transform.position + transform.TransformDirection(radarOffset);
            float lookAheadDistance = Mathf.Abs(currentSpeed * Time.fixedDeltaTime) + stopBufferDistance;

            RaycastHit hit;
            if (Physics.BoxCast(radarCenter, radarHalfExtents, moveDirection, out hit, transform.rotation, lookAheadDistance, obstacleLayer))
            {
                if (hit.collider.CompareTag("inviswall"))
                {
                    currentSpeed = 0f; // Block forward/backward movement
                }
            }
        }

        // Final Position Update
        Vector3 newPosition = rb.position + (transform.forward * currentSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);
    }
    // --- VISUAL DEBUGGING ---
    // This draws the invisible radar box in your Scene View so you can perfectly size it!
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f); // Semi-transparent red

        // Get the center point of the box
        Vector3 radarCenter = transform.position + transform.TransformDirection(radarOffset);

        // Draw a cube representing our BoxCast dimensions
        Gizmos.matrix = Matrix4x4.TRS(radarCenter, transform.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, radarHalfExtents * 2f);
    }
}