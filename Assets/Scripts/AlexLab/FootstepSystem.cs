using UnityEngine;

public class FootstepSystem : MonoBehaviour
{
    public enum FootstepMode
    {
        Outdoor,
        Researchbase,
        Ice,
        Cave,
        Vehicle
    }

    [Header("Audio")]
    public AudioSource footstepSource;

    [Header("Footstep Sounds")]
    public AudioClip[] outdoorSteps;
    public AudioClip[] researchbaseSteps;
    public AudioClip[] iceSteps;
    public AudioClip[] caveSteps;
    public AudioClip[] vehicleSteps;

    [Header("Timing")]
    public float stepInterval = 0.4f;
    private float stepTimer;

    [Header("Current Mode")]
    public FootstepMode currentMode = FootstepMode.Outdoor;

    void Update()
    {
        HandleFootsteps();
    }

    void HandleFootsteps()
    {
        bool isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                        Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

        if (isMoving)
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

    void PlayStep()
    {
        AudioClip[] clips = GetCurrentClips();

        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        footstepSource.PlayOneShot(clip, Random.Range(0.85f, 1.15f));
    }

    AudioClip[] GetCurrentClips()
    {
        switch (currentMode)
        {
            case FootstepMode.Researchbase:
                return researchbaseSteps;

            case FootstepMode.Ice:
                return iceSteps;

            case FootstepMode.Cave:
                return caveSteps;

            case FootstepMode.Vehicle:
                return vehicleSteps;

            default:
                return outdoorSteps;
        }
    }

    // 🔥 Public function to change mode from other scripts
    public void SetFootstepMode(FootstepMode mode)
    {
        currentMode = mode;
    }
}