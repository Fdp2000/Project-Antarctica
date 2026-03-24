using UnityEngine;

public class ItemWobble : MonoBehaviour
{
    [Header("Walking Wobble (Bob)")]
    [Tooltip("How fast the item wobbles back and forth.")]
    public float bobSpeed = 12f;
    [Tooltip("How far it moves left and right.")]
    public float bobAmountX = 0.015f;
    [Tooltip("How far it moves up and down.")]
    public float bobAmountY = 0.015f;

    [Header("Look Sway (Inertia)")]
    [Tooltip("How much the item drags behind when you move your mouse.")]
    public float swayAmount = 0.02f;
    [Tooltip("Prevents the item from flying off-screen if you flick the mouse too fast.")]
    public float maxSway = 0.06f;

    [Header("Smoothing")]
    [Tooltip("How snappy or floaty the item feels.")]
    public float smoothSpeed = 8f;

    private Vector3 startPosition;
    private float timer = 0f;

    void Awake()
    {
        // Remember exactly where you placed it in the Inspector
        startPosition = transform.localPosition;
    }

    void OnEnable()
    {
        // Reset the timer and position instantly when the tape is pulled out
        timer = 0f;
        transform.localPosition = startPosition;
    }

    void Update()
    {
        // --- 1. WALKING WOBBLE ---
        // Get raw input so it reacts instantly to the keyboard
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        Vector3 bobOffset = Vector3.zero;

        // If the player is holding down any movement keys
        if (Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveY) > 0.1f)
        {
            timer += Time.deltaTime * bobSpeed;

            // The magic math! Sin for X and Cos*2 for Y creates a perfect "Figure-8" walking pattern
            bobOffset.x = Mathf.Sin(timer) * bobAmountX;
            bobOffset.y = Mathf.Cos(timer * 2f) * bobAmountY;
        }
        else
        {
            // Reset the timer back to 0 when standing still
            timer = 0f;
        }

        // --- 2. CAMERA LOOK SWAY ---
        // Opposite of mouse movement to simulate weight dragging behind
        float mouseX = -Input.GetAxis("Mouse X") * swayAmount;
        float mouseY = -Input.GetAxis("Mouse Y") * swayAmount;

        // Clamp the values so it doesn't break the camera view
        mouseX = Mathf.Clamp(mouseX, -maxSway, maxSway);
        mouseY = Mathf.Clamp(mouseY, -maxSway, maxSway);

        Vector3 swayOffset = new Vector3(mouseX, mouseY, 0);

        // --- 3. APPLY FINAL POSITION ---
        // Add the base position, the walking wobble, and the mouse sway together
        Vector3 targetPosition = startPosition + bobOffset + swayOffset;

        // Smoothly glide the tape to the calculated target
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * smoothSpeed);
    }
}