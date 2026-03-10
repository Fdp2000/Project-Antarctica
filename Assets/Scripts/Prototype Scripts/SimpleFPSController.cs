using UnityEngine;
using UnityEngine.UI; // Needed for the Crosshair Image

[RequireComponent(typeof(CharacterController))]
public class SimpleFPSController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5.0f;

    [Header("Look Settings")]
    public float lookSensitivity = 2.0f;
    public float lookXLimit = 85.0f;

    [Header("Interaction Settings")]
    public float interactRange = 4.0f;
    public KeyCode interactKey = KeyCode.E;
    public Image crosshairDot; // Drag your UI Image here
    public Material highlightMaterial; // Drag your 'Knobhighligt' material here

    public Vector3 scienceSeatOffset = new Vector3(0, 1.5f, 0);
    public Vector3 drivingSeatOffset = new Vector3(0, 1.5f, 0);

    private Outline currentOutline;

    [Header("Player State (Read Only)")]
    public bool hasCassette = false;
    public GameObject heldCassetteVisual; // Drag a cassette model that is a child of the camera here (lower left)
    public bool isDoingScience = false;

    public Camera playerCamera;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    private bool isSeated = false;
    private Transform currentSeat;
    private Vector3 unseatLocalPosition;
    private VehicleController currentVehicle;

    // Feedback Variables
    private GameObject lastHighlightedObject;
    private Material originalMaterial;
    private Renderer[] highlightedRenderers;
    private Material[] originalMaterials;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (crosshairDot != null) crosshairDot.enabled = true;
    }

    void Update()
    {
        // 1. MOUSE LOOK (Disabled if the script is disabled by KnobInteraction)
        rotationX += -Input.GetAxis("Mouse Y") * lookSensitivity;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * lookSensitivity);

        if (Input.GetKeyDown(interactKey) && isSeated)
        {
            GetUp();
            return;
        }

        // 2. ALWAYS HANDLE VISUAL FEEDBACK
        HandleInteractions();

        if (!isSeated)
        {
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);

            float curSpeedX = walkSpeed * Input.GetAxis("Vertical");
            float curSpeedY = walkSpeed * Input.GetAxis("Horizontal");

            moveDirection = (forward * curSpeedX) + (right * curSpeedY);
            moveDirection.y -= 9.81f * Time.deltaTime;

            characterController.Move(moveDirection * Time.deltaTime);
        }
    }

    private void HandleInteractions()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            GameObject hitObj = hit.collider.gameObject;

            // Highlight Logic for Knobs/Winch/Cassettes
            bool shouldHighlight = false;
            if (hitObj.CompareTag("Winch") || hitObj.name.Contains("Knob")) shouldHighlight = true;
            
            // Highlight the tape if we look at it
            if (hitObj.GetComponent<CassetteInteractable>() != null) shouldHighlight = true;
            
            // Only highlight the Tape Drive if we actually have the tape to put in it!
            CassetteReceiver receiver = hitObj.GetComponent<CassetteReceiver>();
            if (receiver != null && hasCassette && !receiver.hasCassette) shouldHighlight = true;

            if (shouldHighlight)
            {
                ApplyHighlight(hitObj);
            }
            else
            {
                ClearHighlight();
            }

            // --- EXISTING INTERACTION LOGIC ---
            if (hit.collider.CompareTag("Seat"))
            {
                if (Input.GetKeyDown(interactKey)) SitDown(hit.transform, false);
            }
            else if (hit.collider.CompareTag("ScienceStation"))
            {
                if (Input.GetKeyDown(interactKey))
                {
                    if (hasCassette) SitDown(hit.transform, true);
                }
            }
            
            // Component-based Cassette pickup (Replacing 'CoreData' tag)
            CassetteInteractable cassette = hit.collider.GetComponent<CassetteInteractable>();
            if (cassette != null && Input.GetKeyDown(interactKey))
            {
                hasCassette = true;
                if (heldCassetteVisual != null) heldCassetteVisual.SetActive(true);
                Destroy(hit.collider.gameObject);
                Debug.Log("<color=orange>Cassette Picked Up!</color>");
            }
            
            // Inserting the Cassette into the Receiver
            if (receiver != null && Input.GetKeyDown(interactKey))
            {
                if (hasCassette && !receiver.hasCassette)
                {
                    hasCassette = false;
                    if (heldCassetteVisual != null) heldCassetteVisual.SetActive(false);
                    receiver.InsertCassette();
                }
                else if (!hasCassette && !receiver.hasCassette)
                {
                    Debug.Log("I need to find the cassette first...");
                }
            }
            else if (hit.collider.CompareTag("Winch"))
            {
                WinchController winch = hit.collider.GetComponent<WinchController>();
                if (winch != null)
                {
                    if (Input.GetKey(interactKey)) winch.InteractWinch();
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
        // Check if the object we are looking at has an Outline component attached
        Outline outline = obj.GetComponentInParent<Outline>();

        if (outline != null)
        {
            // If we were looking at a different outline, turn it off first
            if (currentOutline != null && currentOutline != outline)
            {
                currentOutline.enabled = false;
            }

            // Turn on the new outline
            currentOutline = outline;
            currentOutline.enabled = true;

            if (crosshairDot != null) crosshairDot.color = Color.green;
        }
    }

    void ClearHighlight()
    {
        if (currentOutline != null)
        {
            // Turn off the outline when we look away
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