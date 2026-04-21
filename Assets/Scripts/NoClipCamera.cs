using UnityEngine;

public class NoClipCamera : MonoBehaviour
{
    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public bool invertY = false;

    [Header("Movement Settings")]
    public float normalSpeed = 10f;
    [Tooltip("Multiplier applied when holding the Fast Key")]
    public float fastSpeedMultiplier = 3f;
    [Tooltip("Multiplier applied when holding the Slow Key")]
    public float slowSpeedMultiplier = 0.3f;

    [Header("Key Bindings")]
    public KeyCode forwardKey = KeyCode.W;
    public KeyCode backwardKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode upKey = KeyCode.E;
    public KeyCode downKey = KeyCode.Q;
    public KeyCode fastKey = KeyCode.LeftShift;
    public KeyCode slowKey = KeyCode.LeftControl;
    [Tooltip("Press this to unlock your mouse cursor and stop moving")]
    public KeyCode toggleMouseCursorKey = KeyCode.Escape;

    private float rotationX = 0.0f;
    private float rotationY = 0.0f;
    private bool cursorLocked = true;

    void Start()
    {
        // Lock the cursor to the center of the screen so you can look around freely
        LockCursor(true);

        // Sync the internal math with wherever the camera is currently looking in the scene
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;
    }

    void Update()
    {
        // Toggle cursor lock so you can actually click on the Unity Editor buttons
        if (Input.GetKeyDown(toggleMouseCursorKey))
        {
            cursorLocked = !cursorLocked;
            LockCursor(cursorLocked);
        }

        // If the cursor is unlocked, freeze the camera so we don't accidentally fly away
        if (!cursorLocked) return;

        // --- MOUSE LOOK MATH ---
        rotationX += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        rotationY += Input.GetAxisRaw("Mouse Y") * mouseSensitivity * (invertY ? 1 : -1);

        // Stop the camera from flipping upside down
        rotationY = Mathf.Clamp(rotationY, -90f, 90f);

        transform.localRotation = Quaternion.Euler(rotationY, rotationX, 0);

        // --- MOVEMENT MATH ---
        float currentSpeed = normalSpeed;

        // Check for speed modifiers
        if (Input.GetKey(fastKey)) currentSpeed *= fastSpeedMultiplier;
        if (Input.GetKey(slowKey)) currentSpeed *= slowSpeedMultiplier;

        Vector3 moveDirection = Vector3.zero;

        // Calculate direction based on where the camera is currently looking
        if (Input.GetKey(forwardKey)) moveDirection += transform.forward;
        if (Input.GetKey(backwardKey)) moveDirection -= transform.forward;
        if (Input.GetKey(leftKey)) moveDirection -= transform.right;
        if (Input.GetKey(rightKey)) moveDirection += transform.right;

        // Up and Down are hardcoded to world space (or local up) so you don't fly diagonally
        if (Input.GetKey(upKey)) moveDirection += transform.up;
        if (Input.GetKey(downKey)) moveDirection -= transform.up;

        // Apply the movement
        transform.position += moveDirection.normalized * currentSpeed * Time.deltaTime;
    }

    private void LockCursor(bool lockState)
    {
        if (lockState)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}