using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class SimpleFPSController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5.0f;
    public float crouchSpeed = 2.5f;
    public float gravity = 30.0f;
    public float lookSensitivity = 2.0f;
    public float lookXLimit = 85.0f;

    [Header("Crouch Settings")]
    public float standingHeight = 2.0f;
    public float crouchHeight = 1.0f;
    public Vector3 standingCameraOffset = new Vector3(0, 0.9f, 0);
    public Vector3 crouchingCameraOffset = new Vector3(0, 0.0f, 0);
    public LayerMask obstacleLayers = Physics.DefaultRaycastLayers;
    public float crouchTransitionSpeed = 10f;

    [Header("Interaction Ranges")]
    public float interactRange = 3.0f;
    public float cassettePickupRange = 2.0f;
    public float winchInteractRange = 4.0f;
    public float noteReadRange = 4.0f;

    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;
    public Image crosshairDot;
    public LayerMask interactionMask = ~0;

    [Header("Seated Settings")]
    public Vector3 drivingSeatOffset = new Vector3(0, 0.35f, -0.3f);
    public Vector3 exitOffset = new Vector3(-1.5f, 0, 0);
    public float seatedLookYLimit = 110.0f;

    [Header("Snapping Rotation")]
    public float enterRotationX = 20f;

    [Header("Cabin & Engine Shake System")]
    public bool isInCabin = false;
    public VehicleController vehicleController;
    public float engineShakeAmount = 0.003f;
    public float engineShakeSpeed = 25f;

    [Header("Player State")]
    public bool hasCassette = false;
    public RadioBeacon currentlyHeldTapeBeacon;
    public GameObject heldCassetteVisual;
    public Camera playerCamera;

    private CharacterController characterController;
    private float rotationX = 0;
    private float rotationY = 0;
    public bool isSeated = false;
    private bool isCrouching = false;
    private Transform currentSeat;
    private float verticalVelocity = 0f;
    private Outline currentOutline;

    private Vector3 smoothCameraOffset;

    [Header("Footsteps")]
    public AudioSource footstepAudioSource;
    public AudioClip[] footstepClips; // Array so you can add multiple snow crunch sounds!

    [Tooltip("How far the player physically moves before a step triggers.")]
    public float walkStepDistance = 2.0f;
    public float crouchStepDistance = 1.5f;

    [Range(0f, 1f)] public float footstepVolume = 0.5f;
    public float basePitch = 1.0f;
    public float pitchVariance = 0.15f;
    public LayerMask footstepGroundLayer = ~0; // What layers make a sound?

    // Add this private tracker with your other private variables:
    private float stepCycle = 0f;
    [Tooltip("Minimum time in seconds between footstep sounds to prevent rapid-fire spam.")]
    public float minStepCooldown = 0.25f;
    [Tooltip("How far the player must physically move before the VERY FIRST step sounds. (0 = Instant)")]
    public float firstStepDistanceBuffer = 0.2f; // <-- NEW

    // Add this private timer next to your private 'stepCycle' variable
    private float stepCooldownTimer = 0f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (PlayerPrefs.HasKey("PlayerSensitivity"))
        {
            lookSensitivity = PlayerPrefs.GetFloat("PlayerSensitivity");
        }
        else
        {
            lookSensitivity = 2.0f; // The safe default for 800 DPI users
        }

        if (heldCassetteVisual != null) heldCassetteVisual.SetActive(false);
        smoothCameraOffset = standingCameraOffset;
        playerCamera.transform.localPosition = smoothCameraOffset;
    }

    void Update()
    {
        if (NoteViewer.Instance != null && NoteViewer.Instance.isReading)
        {
            if (Input.GetKeyDown(interactKey) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Escape))
            {
                NoteViewer.Instance.CloseNote();
            }
            // Don't rotate camera, move player, or highlight objects
            return;
        }
        rotationX += -Input.GetAxis("Mouse Y") * lookSensitivity;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        if (!isSeated)
        {
            transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * lookSensitivity);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            HandleCrouch();
        }
        else
        {
            rotationY += Input.GetAxis("Mouse X") * lookSensitivity;
            rotationY = Mathf.Clamp(rotationY, -seatedLookYLimit, seatedLookYLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
        }

        if (Input.GetKeyDown(interactKey) && isSeated) { GetUp(); return; }
        HandleInteractions();

        if (!isSeated)
        {
            float currentSpeed = isCrouching ? crouchSpeed : walkSpeed;
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);

            // --- THE FIX: Clamp the input so diagonal movement isn't faster! ---
            Vector2 inputDir = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            if (inputDir.magnitude > 1f)
            {
                inputDir.Normalize();
            }

            Vector3 move = (forward * inputDir.y + right * inputDir.x) * currentSpeed;

            if (characterController.isGrounded) verticalVelocity = -2.0f;
            else verticalVelocity -= gravity * Time.deltaTime;

            move.y = verticalVelocity;
            characterController.Move(move * Time.deltaTime);

            HandleFootsteps();
        }

        // --- NEW: Apply the Engine Camera Shake ---
        Vector3 finalShake = Vector3.zero;
        if (isInCabin && vehicleController != null && vehicleController.isEngineRunning)
        {
            float noiseX = (Mathf.PerlinNoise(Time.time * engineShakeSpeed, 0f) - 0.5f) * engineShakeAmount;
            float noiseY = (Mathf.PerlinNoise(0f, Time.time * engineShakeSpeed) - 0.5f) * engineShakeAmount;
            finalShake = new Vector3(noiseX, noiseY, 0f);
        }

        if (isSeated)
        {
            playerCamera.transform.localPosition = finalShake; // Base offset is zero when seated
        }
        else
        {
            playerCamera.transform.localPosition = smoothCameraOffset + finalShake;
        }
    }

    private void HandleCrouch()
    {
        // --- THE FIX: Check for either C or Left Shift ---
        bool crouchInputHeld = Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftShift);

        bool isBlockedAbove = false;
        if (!crouchInputHeld)
        {
            Vector3 rayStart = transform.position + Vector3.up * characterController.radius;
            float rayLength = standingHeight - characterController.radius * 2;
            if (Physics.SphereCast(rayStart, characterController.radius, Vector3.up, out RaycastHit hit, rayLength, obstacleLayers))
            {
                isBlockedAbove = true;
            }
        }

        isCrouching = crouchInputHeld || isBlockedAbove;

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        Vector3 targetCameraOffset = isCrouching ? crouchingCameraOffset : standingCameraOffset;

        characterController.height = Mathf.Lerp(characterController.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        characterController.center = new Vector3(0, characterController.height / 2f, 0);

        // Track the smooth offset rather than applying it instantly so we can add shake to it
        smoothCameraOffset = Vector3.Lerp(smoothCameraOffset, targetCameraOffset, Time.deltaTime * crouchTransitionSpeed);
    }

    private void HandleInteractions()
    {
          // --- NEW: Skip all interaction while reading a note ---
        if (NoteViewer.Instance != null && NoteViewer.Instance.isReading)
        {
        ClearHighlight();  // Optional: remove any highlight when note is open
        return;
        }   
        
        float maxRayRange = Mathf.Max(interactRange, Mathf.Max(cassettePickupRange, winchInteractRange));
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (isSeated)
        {
            Physics.SyncTransforms();
        }

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayRange, interactionMask))
        {
            GameObject target = hit.collider.gameObject;
            float distanceToTarget = hit.distance;

            // ==========================================
            // --- NEW: IGNORE THE CURRENT DRIVER SEAT ---
            // If we are seated and looking at our own seat, hide the outline and ignore it!
            // ==========================================
            if (isSeated && target.transform == currentSeat)
            {
                ClearHighlight();
                return;
            }

            bool interactDown = Input.GetKeyDown(interactKey) || Input.GetMouseButtonDown(0);
            bool interactHeld = Input.GetKey(interactKey) || Input.GetMouseButton(0);

            var note = target.GetComponent<NoteInteract>();
            if (note != null)
            {
                if (distanceToTarget <= noteReadRange)
                {
                    HighlightObject(target);
                    if (Input.GetKeyDown(interactKey))
                    {
                        ClearHighlight();
                        note.Interact();
                    }
                }
                else { ClearHighlight(); }
                return;
            }

            var cassette = target.GetComponent<CassetteInteractable>();
            if (cassette != null)
            {
                if (distanceToTarget <= cassettePickupRange)
                {
                    HighlightObject(target);
                    if (Input.GetKeyDown(interactKey) && !hasCassette) PickUpTape(cassette);
                }
                else { ClearHighlight(); }
                return;
            }

            var punchcard = target.GetComponent<PunchcardInteractable>();
            if (punchcard != null)
            {
                if (distanceToTarget <= cassettePickupRange)
                {
                    HighlightObject(target);
                    if (Input.GetKeyDown(interactKey)) PickUpPunchcard(punchcard);
                }
                else { ClearHighlight(); }
                return;
            }
            var finalStarter = target.GetComponent<FinalMachineStarter>();
            if (finalStarter != null)
            {
                if (distanceToTarget <= interactRange)
                {
                    HighlightObject(target);
                    if (Input.GetKeyDown(interactKey))
                    {
                        ClearHighlight();
                        finalStarter.Interact();
                    }
                }
                else { ClearHighlight(); }
                return;
            }
            // ==========================================
            // --- NEW: DUCK INTERACTABLE ---
            // ==========================================
            var duck = target.GetComponent<DuckInteractable>();
            if (duck != null)
            {
                if (distanceToTarget <= interactRange)
                {
                    HighlightObject(target);
                    // interactDown captures BOTH 'E' and 'Left Click'!
                    if (interactDown)
                    {
                        duck.Interact();
                    }
                }
                else { ClearHighlight(); }
                return;
            }
            // ==========================================


            if (target.CompareTag("Winch"))
            {
                if (distanceToTarget <= winchInteractRange)
                {
                    HighlightObject(target);
                    if (Input.GetKey(interactKey) || Input.GetMouseButton(0))
                    {
                        target.GetComponent<WinchController>()?.InteractWinch();
                    }
                }
                else { ClearHighlight(); }
                return;
            }

            if (distanceToTarget <= interactRange)
            {
                HighlightObject(target);
                bool startInteraction = Input.GetKeyDown(interactKey) || Input.GetMouseButtonDown(0);

                if (startInteraction)
                {
                    var radioKnob = target.GetComponent<KnobInteraction>();
                    if (radioKnob != null) { radioKnob.StartManualDrag(); return; }

                    var sciKnob = target.GetComponent<ScienceKnobInteraction>();
                    if (sciKnob != null) { sciKnob.StartManualDrag(); return; }

                    var finalSciKnob = target.GetComponent<FinalScienceKnobInteraction>();
                    if (finalSciKnob != null) { finalSciKnob.StartManualDrag(); return; }

                    if (Input.GetKeyDown(interactKey))
                    {
                        if (target.CompareTag("Seat") && !hasCassette) SitDown(target.transform);

                        var receiver = target.GetComponent<CassetteReceiver>();
                        if (receiver != null && hasCassette && !receiver.hasCassette)
                        {
                            receiver.InsertCassette(currentlyHeldTapeBeacon);
                            hasCassette = false;
                            if (heldCassetteVisual != null) heldCassetteVisual.SetActive(false);
                        }
                    }
                }
            }
            else { ClearHighlight(); }
        }
        else { ClearHighlight(); }
    }

    private void SitDown(Transform seat)
    {
        isSeated = true;
        currentSeat = seat;
        characterController.enabled = false;

        transform.SetParent(seat);
        transform.localPosition = drivingSeatOffset;
        transform.localRotation = Quaternion.identity;

        rotationX = enterRotationX;
        rotationY = 0;
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);

        if (vehicleController != null) vehicleController.isPlayerDriving = true;

        Physics.SyncTransforms();
    }

    private void GetUp()
    {
        if (currentSeat == null) return;

        Quaternion camWorldRot = playerCamera.transform.rotation;
        Vector3 camForward = camWorldRot * Vector3.forward;
        camForward.y = 0;

        isSeated = false;
        if (vehicleController != null) vehicleController.isPlayerDriving = false;

        transform.SetParent(null);
        Vector3 worldExitOffset = currentSeat.TransformDirection(exitOffset);
        transform.position = currentSeat.position + worldExitOffset;

        transform.rotation = Quaternion.LookRotation(camForward, Vector3.up);
        rotationY = 0;

        smoothCameraOffset = isCrouching ? crouchingCameraOffset : standingCameraOffset;
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

        // --- THE FIX: Force the camera height to update THIS frame! ---
        playerCamera.transform.localPosition = smoothCameraOffset;

        Physics.SyncTransforms();
        characterController.enabled = true;
        Physics.SyncTransforms();
    }
    private void PickUpTape(CassetteInteractable tape)
    {
        hasCassette = true;
        currentlyHeldTapeBeacon = tape.sourceBeacon;

        if (heldCassetteVisual != null)
        {
            heldCassetteVisual.SetActive(true);

            // --- THE DIRECT METHOD ---
            // 1. Find the MeshRenderer (whether it is on the parent or the child)
            MeshRenderer tapeRenderer = heldCassetteVisual.GetComponentInChildren<MeshRenderer>();

            // 2. Directly apply the material from the Beacon!
            if (tapeRenderer != null && tape.sourceBeacon != null && tape.sourceBeacon.uniqueTapeMaterial != null)
            {
                tapeRenderer.material = tape.sourceBeacon.uniqueTapeMaterial;
            }
        }

        Destroy(tape.gameObject);
    }

    private void PickUpPunchcard(PunchcardInteractable card)
    {
        if (card.waveController != null) card.waveController.NotifyPunchcardCollected();

        CassetteReceiver receiver = FindObjectOfType<CassetteReceiver>();
        if (receiver != null) receiver.ConsumeTape();

        Destroy(card.gameObject);
        Debug.Log("Collected Punchcard!");
    }

    void HighlightObject(GameObject obj)
    {
        Outline outline = obj.GetComponent<Outline>() ?? obj.GetComponentInParent<Outline>() ?? obj.GetComponentInChildren<Outline>();
        if (outline != null)
        {
            if (currentOutline != null && currentOutline != outline) currentOutline.enabled = true;
            currentOutline = outline;
            currentOutline.enabled = true;
            if (crosshairDot != null) crosshairDot.color = Color.green;
        }
    }

    void ClearHighlight()
    {
        if (currentOutline != null) currentOutline.enabled = false;
        currentOutline = null;
        if (crosshairDot != null) crosshairDot.color = Color.white;
    }

    public bool IsPunchcardInTray() { return FindObjectOfType<PunchcardInteractable>() != null; }
    private void HandleFootsteps()
    {
        if (stepCooldownTimer > 0f)
        {
            stepCooldownTimer -= Time.deltaTime;
        }

        Vector2 horizontalVelocity = new Vector2(characterController.velocity.x, characterController.velocity.z);

        if (horizontalVelocity.magnitude > 0.1f)
        {
            stepCycle += horizontalVelocity.magnitude * Time.deltaTime;
            float currentStepDistance = isCrouching ? crouchStepDistance : walkStepDistance;

            if (stepCycle >= currentStepDistance && stepCooldownTimer <= 0f)
            {
                stepCycle = 0f;
                PlayFootstepSound();
                stepCooldownTimer = minStepCooldown;
            }
        }
        else
        {
            // --- THE FIX: DELAYED FIRST STEP ---
            // Calculate what the required distance is right now
            float currentStepDistance = isCrouching ? crouchStepDistance : walkStepDistance;

            // Prime the counter, but subtract your custom buffer!
            // (Mathf.Max ensures the math never accidentally goes below 0)
            stepCycle = Mathf.Max(0f, currentStepDistance - firstStepDistanceBuffer);
        }
    }
    private void PlayFootstepSound()
    {
        if (footstepAudioSource == null || footstepClips == null || footstepClips.Length == 0) return;

        // --- THE RAYCAST CHECK ---
        // Start the raycast from the exact center of the player
        Vector3 rayOrigin = transform.position;

        // Shoot it down exactly half the player's current height, plus a tiny 0.2m buffer to hit the floor
        float rayDistance = (characterController.height / 2f) + 0.2f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, footstepGroundLayer))
        {
            // Pick a random sound from the array
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];

            // Apply the pitch variance
            footstepAudioSource.pitch = basePitch + Random.Range(-pitchVariance, pitchVariance);

            // Play it!
            footstepAudioSource.PlayOneShot(clip, footstepVolume);
        }
    }
}