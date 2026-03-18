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
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float standingHeight = 2.0f;
    public float crouchHeight = 1.0f;
    public Vector3 standingCameraOffset = new Vector3(0, 0.9f, 0);
    public Vector3 crouchingCameraOffset = new Vector3(0, 0.0f, 0);
    public LayerMask obstacleLayers = Physics.DefaultRaycastLayers;
    public float crouchTransitionSpeed = 10f;

    [Header("Interaction")]
    public float interactRange = 3.0f;
    public float cassettePickupRange = 2.0f;
    public KeyCode interactKey = KeyCode.E;
    public Image crosshairDot;
    public LayerMask interactionMask = ~0;

    [Header("Seated Settings")]
    public Vector3 drivingSeatOffset = new Vector3(0, 0.35f, -0.3f);
    public Vector3 exitOffset = new Vector3(-1.5f, 0, 0);
    public float seatedLookYLimit = 110.0f;

    [Header("Snapping Rotation")]
    [Tooltip("Head pitch when entering the seat.")]
    public float enterRotationX = 20f;

    [Header("Player State")]
    public bool hasCassette = false;
    public RadioBeacon currentlyHeldTapeBeacon;
    public GameObject heldCassetteVisual;
    public Camera playerCamera;

    private CharacterController characterController;
    private float rotationX = 0;
    private float rotationY = 0;
    private bool isSeated = false;
    private bool isCrouching = false;
    private Transform currentSeat;
    private VehicleController currentVehicle;
    private float verticalVelocity = 0f;
    private Outline currentOutline;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (heldCassetteVisual != null) heldCassetteVisual.SetActive(false);
        playerCamera.transform.localPosition = standingCameraOffset;
    }

    void Update()
    {
        // 1. Camera Look Logic
        rotationX += -Input.GetAxis("Mouse Y") * lookSensitivity;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        if (!isSeated)
        {
            transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * lookSensitivity);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

            // Handle Smooth Crouching
            HandleCrouch();
        }
        else
        {
            rotationY += Input.GetAxis("Mouse X") * lookSensitivity;
            rotationY = Mathf.Clamp(rotationY, -seatedLookYLimit, seatedLookYLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
        }

        // 2. Interaction Logic
        if (Input.GetKeyDown(interactKey) && isSeated) { GetUp(); return; }
        HandleInteractions();

        // 3. Movement & Gravity
        if (!isSeated)
        {
            float currentSpeed = isCrouching ? crouchSpeed : walkSpeed;
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            Vector3 move = (forward * currentSpeed * Input.GetAxis("Vertical")) + (right * currentSpeed * Input.GetAxis("Horizontal"));

            if (characterController.isGrounded) verticalVelocity = -2.0f;
            else verticalVelocity -= gravity * Time.deltaTime;

            move.y = verticalVelocity;
            characterController.Move(move * Time.deltaTime);
        }
    }

    private void HandleCrouch()
    {
        // Check if there is an obstacle above the player's head
        bool isBlockedAbove = false;
        if (!Input.GetKey(crouchKey))
        {
            Vector3 rayStart = transform.position + Vector3.up * characterController.radius;
            float rayLength = standingHeight - characterController.radius * 2;
            if (Physics.SphereCast(rayStart, characterController.radius, Vector3.up, out RaycastHit hit, rayLength, obstacleLayers))
            {
                isBlockedAbove = true;
            }
        }

        // Keep crouching if the key is held OR if we are blocked from standing up
        isCrouching = Input.GetKey(crouchKey) || isBlockedAbove;

        // Smoothly interpolate height and camera position
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        Vector3 targetCameraOffset = isCrouching ? crouchingCameraOffset : standingCameraOffset;

        characterController.height = Mathf.Lerp(characterController.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

        // Adjust center so the player's feet stay glued to the floor while shrinking
        characterController.center = new Vector3(0, characterController.height / 2f, 0);

        playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, targetCameraOffset, Time.deltaTime * crouchTransitionSpeed);
    }

    private void HandleInteractions()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        float maxRayRange = Mathf.Max(interactRange, cassettePickupRange);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayRange, interactionMask))
        {
            GameObject target = hit.collider.gameObject;
            float distanceToTarget = hit.distance;

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

                        if (target.CompareTag("Winch")) target.GetComponent<WinchController>()?.InteractWinch();
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

        // Ensure camera goes to exactly 0 locally while seated
        playerCamera.transform.localPosition = Vector3.zero;

        currentVehicle = seat.GetComponentInParent<VehicleController>();
        if (currentVehicle != null) currentVehicle.isPlayerDriving = true;

        Physics.SyncTransforms();
    }

    private void GetUp()
    {
        if (currentSeat == null) return;

        // 1. Capture orientation for seamless transition
        Quaternion camWorldRot = playerCamera.transform.rotation;
        Vector3 camForward = camWorldRot * Vector3.forward;
        camForward.y = 0;

        isSeated = false;
        if (currentVehicle != null) currentVehicle.isPlayerDriving = false;

        // 2. Unparent and position
        transform.SetParent(null);
        Vector3 worldExitOffset = currentSeat.TransformDirection(exitOffset);
        transform.position = currentSeat.position + worldExitOffset;

        // 3. Re-apply rotation to body
        transform.rotation = Quaternion.LookRotation(camForward, Vector3.up);
        rotationY = 0;

        // 4. Restore character camera position and sync
        // We set it to the target offset directly to prevent a "lerp slide" on frame 1
        playerCamera.transform.localPosition = isCrouching ? crouchingCameraOffset : standingCameraOffset;
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

        Physics.SyncTransforms();
        characterController.enabled = true;
        Physics.SyncTransforms(); // Double sync to kill the frame glitch
    }

    private void PickUpTape(CassetteInteractable tape)
    {
        hasCassette = true;
        currentlyHeldTapeBeacon = tape.sourceBeacon;
        if (heldCassetteVisual != null) heldCassetteVisual.SetActive(true);
        Destroy(tape.gameObject);
    }

    void HighlightObject(GameObject obj)
    {
        Outline outline = obj.GetComponent<Outline>() ?? obj.GetComponentInParent<Outline>();
        if (outline != null)
        {
            if (currentOutline != null && currentOutline != outline) currentOutline.enabled = false;
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
}