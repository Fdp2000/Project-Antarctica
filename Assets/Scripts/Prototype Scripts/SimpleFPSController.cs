using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class SimpleFPSController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5.0f;
    public float crouchSpeed = 2.5f;
    public float gravity = 9.81f;

    [Header("Crouch Settings")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchHeight = 1.0f;
    // --- NEW: Custom layer mask so we only check against solid walls, not triggers
    public LayerMask obstacleLayers = Physics.DefaultRaycastLayers;

    [Header("Camera Settings")]
    // --- NEW: Easily adjust the exact camera height from the Inspector
    public Vector3 standingCameraOffset = new Vector3(0, 0.6f, 0);
    public Vector3 crouchingCameraOffset = new Vector3(0, 0.2f, 0);

    [Header("Look Settings")]
    public float lookSensitivity = 2.0f;
    public float lookXLimit = 85.0f;

    [Header("Interaction Settings")]
    public float interactRange = 4.0f;
    public KeyCode interactKey = KeyCode.E;
    public Image crosshairDot;
    public Material highlightMaterial;

    public Vector3 scienceSeatOffset = new Vector3(0, 1.5f, 0);
    public Vector3 drivingSeatOffset = new Vector3(0, 1.5f, 0);

    private Outline currentOutline;

    [Header("Player State (Read Only)")]
    public bool hasCassette = false;
    public RadioBeacon currentlyHeldTapeBeacon;
    public GameObject heldCassetteVisual;
    public bool isDoingScience = false;

    public Camera playerCamera;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    private bool isSeated = false;
    private Transform currentSeat;
    private Vector3 unseatLocalPosition;
    private VehicleController currentVehicle;

    // --- Crouch State Variables ---
    private float standingHeight;
    private Vector3 standingCenter;
    private bool isCrouching = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (crosshairDot != null) crosshairDot.enabled = true;

        // Memorize default standing data
        standingHeight = characterController.height;
        standingCenter = characterController.center;

        // Force the camera to start at the exact standing offset you choose
        if (playerCamera != null) playerCamera.transform.localPosition = standingCameraOffset;
    }

    void Update()
    {
        // Look logic
        rotationX += -Input.GetAxis("Mouse Y") * lookSensitivity;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * lookSensitivity);

        if (Input.GetKeyDown(interactKey) && isSeated)
        {
            GetUp();
            return;
        }

        HandleInteractions();

        // --- CROUCH LOGIC (HOLD & CAPSULE CHECK) ---
        if (!isSeated)
        {
            bool isHoldingCrouch = Input.GetKey(crouchKey);

            if (isHoldingCrouch && !isCrouching)
            {
                SetCrouchState(true);
            }
            else if (!isHoldingCrouch && isCrouching)
            {
                if (CanStandUp())
                {
                    SetCrouchState(false);
                }
            }
        }

        // --- MOVEMENT & GRAVITY LOGIC ---
        if (!isSeated)
        {
            float currentY = moveDirection.y;
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);

            float activeSpeed = isCrouching ? crouchSpeed : walkSpeed;
            float curSpeedX = activeSpeed * Input.GetAxis("Vertical");
            float curSpeedY = activeSpeed * Input.GetAxis("Horizontal");

            moveDirection = (forward * curSpeedX) + (right * curSpeedY);

            if (characterController.isGrounded)
            {
                moveDirection.y = -2.0f;
            }
            else
            {
                moveDirection.y = currentY - (gravity * Time.deltaTime);
            }

            characterController.Move(moveDirection * Time.deltaTime);
        }
    }

    // --- UPDATED: The Bulletproof Capsule Check ---
    private bool CanStandUp()
    {
        // Math to figure out the top and bottom of the player's standing body
        Vector3 topPoint = transform.position + standingCenter + Vector3.up * (standingHeight / 2f - characterController.radius);
        Vector3 bottomPoint = transform.position + standingCenter - Vector3.up * (standingHeight / 2f - characterController.radius);

        characterController.enabled = false;

        // Check if anything solid is sitting inside where our body wants to be
        // QueryTriggerInteraction.Ignore ensures we don't get blocked by invisible trigger zones!
        bool isObstructed = Physics.CheckCapsule(bottomPoint, topPoint, characterController.radius * 0.95f, obstacleLayers, QueryTriggerInteraction.Ignore);

        characterController.enabled = true;

        return !isObstructed;
    }

    private void SetCrouchState(bool state)
    {
        isCrouching = state;

        if (isCrouching)
        {
            characterController.height = crouchHeight;
            characterController.center = standingCenter - new Vector3(0, (standingHeight - crouchHeight) / 2f, 0);

            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = crouchingCameraOffset;
            }
        }
        else
        {
            characterController.height = standingHeight;
            characterController.center = standingCenter;

            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = standingCameraOffset;
            }
        }
    }

    // ... [Interaction Code remains exactly the same below] ...
    public bool IsPunchcardInTray()
    {
        return FindObjectOfType<PunchcardInteractable>() != null;
    }

    private void HandleInteractions()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            GameObject hitObj = hit.collider.gameObject;
            bool shouldHighlight = false;

            if (hitObj.CompareTag("Winch") || hitObj.name.Contains("Knob")) shouldHighlight = true;
            if (hitObj.GetComponent<CassetteInteractable>() != null) shouldHighlight = true;

            PunchcardInteractable punchcard = hitObj.GetComponent<PunchcardInteractable>();
            if (punchcard != null) shouldHighlight = true;

            CassetteReceiver receiver = hitObj.GetComponent<CassetteReceiver>();
            if (receiver != null && hasCassette && !receiver.hasCassette) shouldHighlight = true;

            if (shouldHighlight) ApplyHighlight(hitObj);
            else ClearHighlight();

            if (hitObj.CompareTag("Winch"))
            {
                WinchController winch = hitObj.GetComponent<WinchController>();
                if (winch != null && Input.GetKey(interactKey))
                {
                    winch.InteractWinch();
                }
            }

            if (Input.GetKeyDown(interactKey))
            {
                if (punchcard != null)
                {
                    if (punchcard.waveController != null) punchcard.waveController.NotifyPunchcardCollected();
                    Destroy(hitObj);
                    return;
                }

                CassetteInteractable cassette = hitObj.GetComponent<CassetteInteractable>();
                if (cassette != null)
                {
                    hasCassette = true;
                    currentlyHeldTapeBeacon = cassette.sourceBeacon;
                    if (heldCassetteVisual != null) heldCassetteVisual.SetActive(true);
                    Destroy(hitObj);
                    return;
                }

                if (hit.collider.CompareTag("Seat")) SitDown(hit.transform, false);
                else if (hit.collider.CompareTag("ScienceStation") && hasCassette) SitDown(hit.transform, true);
                else if (receiver != null && hasCassette && !receiver.hasCassette)
                {
                    hasCassette = false;
                    if (heldCassetteVisual != null) heldCassetteVisual.SetActive(false);
                    receiver.InsertCassette(currentlyHeldTapeBeacon);
                    currentlyHeldTapeBeacon = null;
                }
            }
        }
        else
        {
            ClearHighlight();
        }
    }

    void ApplyHighlight(GameObject obj)
    {
        Outline outline = obj.GetComponentInParent<Outline>();
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
        if (currentOutline != null)
        {
            currentOutline.enabled = false;
            currentOutline = null;
            if (crosshairDot != null) crosshairDot.color = Color.white;
        }
    }

    private void GetUp()
    {
        isSeated = false;
        isDoingScience = false;
        if (currentVehicle != null)
        {
            currentVehicle.isPlayerDriving = false;
            currentVehicle = null;
        }
        Vector3 newStandPosition = currentSeat.TransformPoint(unseatLocalPosition);
        transform.SetParent(null);
        transform.position = newStandPosition;
        currentSeat = null;
        characterController.enabled = true;
    }

    private void SitDown(Transform seatTransform, bool scienceMode)
    {
        isSeated = true;
        isDoingScience = scienceMode;
        currentSeat = seatTransform;
        unseatLocalPosition = seatTransform.InverseTransformPoint(transform.position);
        characterController.enabled = false;
        transform.SetParent(currentSeat);
        transform.localPosition = isDoingScience ? scienceSeatOffset : drivingSeatOffset;
        transform.localRotation = Quaternion.identity;

        if (!isDoingScience)
        {
            currentVehicle = currentSeat.GetComponentInParent<VehicleController>();
            if (currentVehicle != null) currentVehicle.isPlayerDriving = true;
        }
    }
}