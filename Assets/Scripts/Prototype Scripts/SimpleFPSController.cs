using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleFPSController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3.0f;

    [Header("Look Settings")]
    public float lookSensitivity = 2.0f;
    public float lookXLimit = 85.0f;

    [Header("Interaction Settings")]
    public float interactRange = 3.0f;
    public KeyCode interactKey = KeyCode.E;

    public Camera playerCamera;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    // State Variables
    private bool isSeated = false;
    private Transform currentSeat;
    private Vector3 unseatPosition;
    private VehicleController currentVehicle; // Reference to the vehicle

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

        // Use Rotate instead of Quaternion math so it plays nice when parented to a turning vehicle
        transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * lookSensitivity);

        // 2. INTERACTION 
        if (Input.GetKeyDown(interactKey))
        {
            if (isSeated) GetUp();
            else TryInteract();
        }

        // 3. WALKING
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

    private void TryInteract()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            if (hit.collider.CompareTag("Seat"))
            {
                SitDown(hit.transform);
            }
        }
    }

    private void SitDown(Transform seatTransform)
    {
        isSeated = true;
        currentSeat = seatTransform;
        unseatPosition = transform.position;
        characterController.enabled = false;

        // Parent the player to the seat so we move and rotate with the vehicle
        transform.SetParent(currentSeat);

        // Snap the player into a sitting position relative to the seat
        transform.localPosition = new Vector3(0, 2f, 0);
        transform.localRotation = Quaternion.identity; // Snap facing forward

        // Tell the vehicle to start listening to the keyboard
        currentVehicle = currentSeat.GetComponentInParent<VehicleController>();
        if (currentVehicle != null)
        {
            currentVehicle.isPlayerDriving = true;
        }
    }

    private void GetUp()
    {
        isSeated = false;
        currentSeat = null;

        // Tell the vehicle to stop listening to the keyboard
        if (currentVehicle != null)
        {
            currentVehicle.isPlayerDriving = false;
            currentVehicle = null;
        }

        // Unparent the player and put them back where they stood
        transform.SetParent(null);
        transform.position = unseatPosition;

        // Turn physics back on so we don't fall through the floor
        characterController.enabled = true;
    }
}