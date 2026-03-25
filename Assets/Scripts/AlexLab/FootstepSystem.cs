using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepSystem : MonoBehaviour
{
    [Header("References")]
    public AudioSource footstepSource;
    public SimpleFPSController playerController;

    [Header("Footstep Sounds")]
    public AudioClip[] footstepClips;

    [Header("Timing")]
    public float stepInterval = 0.4f;
    private float stepTimer;

    [Header("Ground Check")]
    public float groundCheckDistance = 1.3f;

    [Header("Randomization")]
    public Vector2 pitchRange = new Vector2(0.9f, 1.1f);
    public Vector2 volumeRange = new Vector2(0.8f, 1.0f);

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleFootsteps();
    }

    void HandleFootsteps()
    {
        // ❌ Don't play footsteps while in vehicle / seated
        if (playerController != null && playerController.isSeated)
            return;

        // ✅ Movement based on velocity
        bool isMoving = controller.velocity.magnitude > 0.2f;

        // ✅ Ground check using raycast
        bool isGrounded = controller.isGrounded && CheckGroundRay();

        if (isMoving && isGrounded)
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                PlayStep();
                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    bool CheckGroundRay()
    {
        Ray ray = new Ray(transform.position, Vector3.down);

        // Detect ANY collider (no layer mask)
        if (Physics.Raycast(ray, out RaycastHit hit, groundCheckDistance))
        {
            // Optional: ignore triggers
            if (hit.collider.isTrigger) return false;

            return true;
        }

        return false;
    }

    void PlayStep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];

        // 🎲 Random pitch & volume
        footstepSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        float volume = Random.Range(volumeRange.x, volumeRange.y);

        footstepSource.PlayOneShot(clip, volume);
    }
}