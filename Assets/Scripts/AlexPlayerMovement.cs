using UnityEngine;
using UnityEngine.InputSystem;

public class AlexPlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public float speed = 6f;
    public float gravity = -19.62f;
    public float jumpHeight = 2f;

    private Vector2 moveInput;
    private Vector3 velocity;

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void Update()
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * speed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        controller.Move(velocity * Time.deltaTime);
    }
}