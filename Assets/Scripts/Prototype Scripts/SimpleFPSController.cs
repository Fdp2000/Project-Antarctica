using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class SimpleFPSController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5.0f;
    public float gravity = 9.81f; // Added a variable for gravity

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

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (crosshairDot != null) crosshairDot.enabled = true;
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

        // --- FIXED MOVEMENT & GRAVITY LOGIC ---
        if (!isSeated)
        {
            // 1. Remember the current vertical velocity
            float currentY = moveDirection.y;

            // 2. Calculate the horizontal movement based on where we are looking
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            float curSpeedX = walkSpeed * Input.GetAxis("Vertical");
            float curSpeedY = walkSpeed * Input.GetAxis("Horizontal");

            // Overwrite moveDirection with horizontal inputs
            moveDirection = (forward * curSpeedX) + (right * curSpeedY);

            // 3. Re-apply the vertical velocity and calculate gravity
            if (characterController.isGrounded)
            {
                // A small downward push keeps the controller smoothly snapped to the floor
                moveDirection.y = -2.0f;
            }
            else
            {
                // If in the air, accumulate gravity over time
                moveDirection.y = currentY - (gravity * Time.deltaTime);
            }

            // 4. Finally, move the player
            characterController.Move(moveDirection * Time.deltaTime);
        }
    }

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

            // --- WINCH INTERACTION (CONTINUOUS HOLD) ---
            if (hitObj.CompareTag("Winch"))
            {
                WinchController winch = hitObj.GetComponent<WinchController>();
                if (winch != null && Input.GetKey(interactKey))
                {
                    winch.InteractWinch();
                }
            }

            // --- SINGLE CLICK INTERACTIONS ---
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