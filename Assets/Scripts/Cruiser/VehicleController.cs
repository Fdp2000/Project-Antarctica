using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [Header("Vehicle Specs (Transform Movement)")]
    public float maxSpeed = 5.0f;
    public float acceleration = 2.0f;
    public float deceleration = 4.0f;
    public float turnSpeed = 45.0f;
    public float turnAcceleration = 90.0f;
    public float turnDeceleration = 120.0f;

    [Header("Engine Audio System")]
    [Tooltip("The low, rumbling hum when the vehicle is locked down and ready to drive.")]
    public AudioSource engineIdleSource;
    [Tooltip("The aggressive mechanical grinding when actually moving the treads.")]
    public AudioSource engineActiveSource;
    public float maxIdleVolume = 0.5f;
    public float maxActiveVolume = 1.0f;
    public float activePitchMultiplier = 1.3f;
    public float engineLerpSpeed = 5.0f;

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
    public CassetteReceiver cassetteReceiver;
    public SimpleFPSController player;

    [HideInInspector] public bool isPlayerDriving = false;
    [HideInInspector] public bool isEngineRunning = false;

    private Rigidbody rb;
    private float currentMoveInput = 0f;
    private float currentTurnInput = 0f;
    private bool isMovementLocked = false;

    private float currentSpeed = 0f;
    private float currentTurnRate = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (steeringWheel != null) initialSteeringRotation = steeringWheel.localRotation;

        // Initialize Audio
        if (engineIdleSource != null) { engineIdleSource.loop = true; engineIdleSource.volume = 0f; engineIdleSource.Play(); }
        if (engineActiveSource != null) { engineActiveSource.loop = true; engineActiveSource.volume = 0f; engineActiveSource.Play(); }
    }

    void Update()
    {
        // 1. Check Mechanical Locks (Are the alarms off?)
        bool doorOpen = (winch != null && !winch.IsDoorClosed);
        bool unfinishedTape = (cassetteReceiver != null && cassetteReceiver.hasCassette);
        bool cardWaiting = (player != null && player.IsPunchcardInTray());
        bool carryingTape = (player != null && player.hasCassette);

        bool isMechanicallyLocked = doorOpen || unfinishedTape || cardWaiting || carryingTape;

        // The engine fires up the second the locks are cleared!
        isEngineRunning = !isMechanicallyLocked;

        // 2. Player Input Logic
        if (isPlayerDriving)
        {
            if (isMechanicallyLocked)
            {
                if (doorOpen) Debug.LogWarning("Drive Locked: Rear door must be closed!");
                if (unfinishedTape) Debug.LogWarning("Drive Locked: Finish the science minigame!");
                if (cardWaiting) Debug.LogWarning("Drive Locked: Collect the punchcard first!");
                if (carryingTape) Debug.LogWarning("Drive Locked: Store the cassette tape before driving!");

                isMovementLocked = true;
                currentMoveInput = 0f;
                currentTurnInput = 0f;
            }
            else
            {
                isMovementLocked = false;
                currentMoveInput = Input.GetAxisRaw("Vertical");
                currentTurnInput = Input.GetAxisRaw("Horizontal");
            }

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

        // 3. Audio Mixing (Lerping)
        if (isEngineRunning)
        {
            if (engineIdleSource) engineIdleSource.volume = Mathf.Lerp(engineIdleSource.volume, maxIdleVolume, Time.deltaTime * engineLerpSpeed);

            // Calculate how hard the vehicle is currently working
            float movementIntensity = Mathf.Clamp01((Mathf.Abs(currentSpeed) / maxSpeed) + (Mathf.Abs(currentTurnRate) / turnSpeed));

            if (engineActiveSource)
            {
                float targetActiveVol = movementIntensity * maxActiveVolume;
                engineActiveSource.volume = Mathf.Lerp(engineActiveSource.volume, targetActiveVol, Time.deltaTime * engineLerpSpeed);

                float targetPitch = 1.0f + (movementIntensity * (activePitchMultiplier - 1.0f));
                engineActiveSource.pitch = Mathf.Lerp(engineActiveSource.pitch, targetPitch, Time.deltaTime * engineLerpSpeed);
            }
        }
        else
        {
            // Engine is off (alarms are on), fade everything to zero
            if (engineIdleSource) engineIdleSource.volume = Mathf.Lerp(engineIdleSource.volume, 0f, Time.deltaTime * engineLerpSpeed * 2f);
            if (engineActiveSource) engineActiveSource.volume = Mathf.Lerp(engineActiveSource.volume, 0f, Time.deltaTime * engineLerpSpeed * 2f);
        }
    }

    void FixedUpdate()
    {
        float targetSpeed = isMovementLocked ? 0f : currentMoveInput * maxSpeed;

        if (Mathf.Abs(targetSpeed) > 0.1f)
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.fixedDeltaTime);

        float targetTurnRate = isMovementLocked ? 0f : currentTurnInput * turnSpeed;

        if (Mathf.Abs(targetTurnRate) > 0.1f)
            currentTurnRate = Mathf.MoveTowards(currentTurnRate, targetTurnRate, turnAcceleration * Time.fixedDeltaTime);
        else
            currentTurnRate = Mathf.MoveTowards(currentTurnRate, 0f, turnDeceleration * Time.fixedDeltaTime);

        Vector3 checkExtents = radarHalfExtents * 0.98f;
        float turnAmount = currentTurnRate * Time.fixedDeltaTime;
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
}