using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [Header("Vehicle Specs (Transform Movement)")]
    public float maxSpeed = 5.0f;
    public float acceleration = 2.0f;
    public float deceleration = 4.0f;
    public float turnSpeed = 45.0f;

    [Header("Obstacle Radar (OverlapBox)")]
    public Vector3 radarOffset = new Vector3(0, 1f, 0);
    public Vector3 radarHalfExtents = new Vector3(2f, 1.5f, 3f);
    public float stopBufferDistance = 0.5f;
    public LayerMask obstacleLayer = ~0;

    [Header("Visuals")]
    public Transform steeringWheel;
    public float maxSteeringAngle = 120.0f;
    private Quaternion initialSteeringRotation;

    [Header("Safety Dependencies")]
    public WinchController winch;
    public CassetteReceiver cassetteReceiver; // MUST DRAG MACHINE HERE IN INSPECTOR
    public SimpleFPSController player;

    [HideInInspector] public bool isPlayerDriving = false;

    private Rigidbody rb;
    private float currentMoveInput = 0f;
    private float currentTurnInput = 0f;
    private bool isMovementLocked = false;
    private float currentSpeed = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (steeringWheel != null) initialSteeringRotation = steeringWheel.localRotation;
    }

    void Update()
    {
        if (isPlayerDriving)
        {
            bool doorOpen = (winch != null && !winch.IsDoorClosed);

            // If the tape is inside, the minigame is in progress (it gets consumed when done)
            bool unfinishedTape = (cassetteReceiver != null && cassetteReceiver.hasCassette);

            bool cardWaiting = (player != null && player.IsPunchcardInTray());
            bool carryingTape = (player != null && player.hasCassette);

            if (doorOpen || unfinishedTape || cardWaiting || carryingTape)
            {
                if (doorOpen) Debug.LogWarning("Drive Locked: Rear door must be closed!");
                if (unfinishedTape) Debug.LogWarning("Drive Locked: Finish the science minigame!");
                if (cardWaiting) Debug.LogWarning("Drive Locked: Collect the punchcard first!");
                if (carryingTape) Debug.LogWarning("Drive Locked: Store the cassette tape before driving!");

                isMovementLocked = true;
                currentMoveInput = 0f;
                currentTurnInput = 0f;
                return;
            }

            isMovementLocked = false;
            currentMoveInput = Input.GetAxisRaw("Vertical");
            currentTurnInput = Input.GetAxisRaw("Horizontal");

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
        float targetSpeed = isMovementLocked ? 0f : currentMoveInput * maxSpeed;

        if (Mathf.Abs(targetSpeed) > 0.1f)
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.fixedDeltaTime);

        Vector3 checkExtents = radarHalfExtents * 0.98f;

        float turnAmount = currentTurnInput * turnSpeed * Time.fixedDeltaTime;
        bool canRotate = true;

        if (Mathf.Abs(turnAmount) > 0.01f)
        {
            Quaternion potentialRotation = rb.rotation * Quaternion.Euler(0, turnAmount, 0);
            Vector3 rotRadarCenter = rb.position + potentialRotation * radarOffset;

            Collider[] rotHits = Physics.OverlapBox(rotRadarCenter, checkExtents, potentialRotation, obstacleLayer);
            if (rotHits.Length > 0) canRotate = false;
        }

        if (!isMovementLocked && canRotate) rb.MoveRotation(rb.rotation * Quaternion.Euler(0, turnAmount, 0));

        if (Mathf.Abs(currentSpeed) > 0.01f)
        {
            Vector3 moveOffset = transform.forward * currentSpeed * Time.fixedDeltaTime;
            Vector3 bufferOffset = (currentSpeed > 0 ? transform.forward : -transform.forward) * stopBufferDistance;
            Vector3 futureRadarCenter = rb.position + moveOffset + transform.TransformDirection(radarOffset) + bufferOffset;

            Collider[] moveHits = Physics.OverlapBox(futureRadarCenter, checkExtents, transform.rotation, obstacleLayer);
            if (moveHits.Length > 0) currentSpeed = 0f;
        }

        Vector3 newPosition = rb.position + (transform.forward * currentSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Vector3 radarCenter = transform.position + transform.TransformDirection(radarOffset);
        Gizmos.matrix = Matrix4x4.TRS(radarCenter, transform.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, radarHalfExtents * 2f);
    }
}