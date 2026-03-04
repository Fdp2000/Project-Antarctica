using UnityEngine;

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

    public Vector3 scienceSeatOffset = new Vector3(0, 1.5f, 0);
    public Vector3 drivingSeatOffset = new Vector3(0, 1.5f, 0);

    [Header("Player State (Read Only)")]
    public bool hasCoreData = false;
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
    }

    void Update()
    {
        // 1. MOUSE LOOK 
        rotationX += -Input.GetAxis("Mouse Y") * lookSensitivity;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * lookSensitivity);

        // 2. CHECK IF GETTING UP
        if (Input.GetKeyDown(interactKey) && isSeated)
        {
            GetUp();
            return; // Stop running code for this frame so we don't accidentally click something else
        }

        // 3. HANDLE INTERACTIONS & WALKING (Only if standing)
        if (!isSeated)
        {
            HandleInteractions();

            // THE FIX: If HandleInteractions() just made us sit down, stop running the movement code for this frame!
            if (isSeated) return;

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

        Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.red);

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            // A. SEAT INTERACTION
            if (hit.collider.CompareTag("Seat"))
            {
                if (Input.GetKeyDown(interactKey)) SitDown(hit.transform, false);
            }

            // B. SCIENCE STATION INTERACTION
            else if (hit.collider.CompareTag("ScienceStation"))
            {
                if (Input.GetKeyDown(interactKey))
                {
                    if (hasCoreData) SitDown(hit.transform, true);
                    else Debug.Log("You need Core Data!");
                }
            }

            // C. CORE DATA INTERACTION
            else if (hit.collider.CompareTag("CoreData"))
            {
                if (Input.GetKeyDown(interactKey))
                {
                    hasCoreData = true;
                    Destroy(hit.collider.gameObject);
                }
            }

            // D. WINCH INTERACTION
            else if (hit.collider.CompareTag("Winch"))
            {
                WinchController winch = hit.collider.GetComponent<WinchController>();

                if (winch != null)
                {
                    if (Input.GetKeyDown(interactKey))
                    {
                        Debug.Log("Winch CLICK detected!");
                        winch.ClickToOpen();
                    }
                    else if (Input.GetKey(interactKey))
                    {
                        winch.HoldToClose();
                    }
                }
                else
                {
                    if (Input.GetKeyDown(interactKey))
                    {
                        Debug.LogWarning("You clicked an object tagged 'Winch', but it is missing the WinchController.cs script!");
                    }
                }
            }
        }
    }

    private void SitDown(Transform seatTransform, bool scienceMode)
    {
        isSeated = true;
        isDoingScience = scienceMode;
        currentSeat = seatTransform;
        unseatLocalPosition = seatTransform.InverseTransformPoint(transform.position);
        characterController.enabled = false;

        transform.SetParent(currentSeat);

        // --- THE FIX ---
        // Pick the right offset based on which seat we clicked
        if (isDoingScience)
        {
            transform.localPosition = scienceSeatOffset;
        }
        else
        {
            transform.localPosition = drivingSeatOffset;
        }
        // ---------------

        transform.localRotation = Quaternion.identity;

        if (!isDoingScience)
        {
            currentVehicle = currentSeat.GetComponentInParent<VehicleController>();
            if (currentVehicle != null) currentVehicle.isPlayerDriving = true;
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
}