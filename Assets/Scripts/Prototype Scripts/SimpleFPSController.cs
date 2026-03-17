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
    public LayerMask obstacleLayers = Physics.DefaultRaycastLayers;

    [Header("Camera Settings")]
    public Vector3 standingCameraOffset = new Vector3(0, 0.6f, 0);
    public Vector3 crouchingCameraOffset = new Vector3(0, 0.2f, 0);

    // --- NEW: Full control over the exact starting camera angle when seated!
    [Tooltip("Pitch: Positive looks DOWN, Negative looks UP")]
    public float defaultSeatedRotationX = 15f; // Starts looking slightly down at the dash
    [Tooltip("Yaw: Positive looks RIGHT, Negative looks LEFT")]
    public float defaultSeatedRotationY = 0f;

    [Header("Look Settings")]
    public float lookSensitivity = 2.0f;
    public float lookXLimit = 85.0f;
    public float seatedLookYLimit = 110.0f;

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
    private float rotationY = 0;

    private bool isSeated = false;
    private Transform currentSeat;
    private Vector3 unseatLocalPosition;
    private VehicleController currentVehicle;

    private float standingHeight;
    private Vector3 standingCenter;
    private bool isCrouching = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (crosshairDot != null) crosshairDot.enabled = true;

        standingHeight = characterController.height;
        standingCenter = characterController.center;

        if (playerCamera != null) playerCamera.transform.localPosition = standingCameraOffset;
    }

    void Update()
    {
        rotationX += -Input.GetAxis("Mouse Y") * lookSensitivity;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        if (!isSeated)
        {
            transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * lookSensitivity);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        }
        else
        {
            rotationY += Input.GetAxis("Mouse X") * lookSensitivity;
            rotationY = Mathf.Clamp(rotationY, -seatedLookYLimit, seatedLookYLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
        }

        if (Input.GetKeyDown(interactKey) && isSeated)
        {
            GetUp();
            return;
        }

        HandleInteractions();

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

    private bool CanStandUp()
    {
        Vector3 topPoint = transform.position + standingCenter + Vector3.up * (standingHeight / 2f - characterController.radius);
        Vector3 bottomPoint = transform.position + standingCenter - Vector3.up * (standingHeight / 2f - characterController.radius);

        characterController.enabled = false;
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

            if (playerCamera != null) playerCamera.transform.localPosition = crouchingCameraOffset;
        }
        else
        {
            characterController.height = standingHeight;
            characterController.center = standingCenter;

            if (playerCamera != null) playerCamera.transform.localPosition = standingCameraOffset;
        }
    }

    public bool IsPunchcardInTray()
    {
        return FindObjectOfType<PunchcardInteractable>() != null;
    }

    private void HandleInteractions()
    {
        GameObject targetObj = null;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            targetObj = hit.collider.gameObject;
        }
        else
        {
            Collider[] overlaps = Physics.OverlapSphere(playerCamera.transform.position, 0.5f);
            foreach (Collider col in overlaps)
            {
                if (col.CompareTag("Seat") || col.CompareTag("ScienceStation"))
                {
                    targetObj = col.gameObject;
                    break;
                }
            }
        }

        if (targetObj != null)
        {
            bool shouldHighlight = false;

            if (targetObj.CompareTag("Winch") || targetObj.name.Contains("Knob")) shouldHighlight = true;
            if (targetObj.GetComponent<CassetteInteractable>() != null) shouldHighlight = true;

            PunchcardInteractable punchcard = targetObj.GetComponent<PunchcardInteractable>();
            if (punchcard != null) shouldHighlight = true;

            CassetteReceiver receiver = targetObj.GetComponent<CassetteReceiver>();
            if (receiver != null && hasCassette && !receiver.hasCassette) shouldHighlight = true;

            if (shouldHighlight) ApplyHighlight(targetObj);
            else ClearHighlight();

            if (targetObj.CompareTag("Winch"))
            {
                WinchController winch = targetObj.GetComponent<WinchController>();
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
                    Destroy(targetObj);
                    return;
                }

                CassetteInteractable cassette = targetObj.GetComponent<CassetteInteractable>();
                if (cassette != null)
                {
                    hasCassette = true;
                    currentlyHeldTapeBeacon = cassette.sourceBeacon;
                    if (heldCassetteVisual != null) heldCassetteVisual.SetActive(true);
                    Destroy(targetObj);
                    return;
                }

                if (targetObj.CompareTag("Seat")) SitDown(targetObj.transform, false);
                else if (targetObj.CompareTag("ScienceStation") && hasCassette) SitDown(targetObj.transform, true);
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

        // Reset Y rotation so you look straight ahead relative to your body
        rotationY = 0f;
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

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

        // --- UPDATED: Snap to BOTH your custom X and Y angles!
        rotationX = defaultSeatedRotationX;
        rotationY = defaultSeatedRotationY;

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