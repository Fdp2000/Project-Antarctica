using UnityEngine;

public class FootstepSystem : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource footstepSource;
    public AudioClip[] indoorSteps;
    public AudioClip[] outdoorSteps;

    [Header("Timing")]
    public float stepInterval = 0.4f;
    private float stepTimer;

    [Header("Indoor Detection")]
    public Collider buildingCollider; // drag your building trigger collider here

    [Header("Wind (optional)")]
    public AudioLowPassFilter windFilter;
    public float indoorCutoff = 3000f;
    public float outdoorCutoff = 22000f;

    private bool isIndoor = false;

    void Update()
    {
        HandleIndoorCheck();
        HandleFootsteps();
    }

    void HandleIndoorCheck()
    {
        if (buildingCollider == null) return;

        bool currentlyIndoor = buildingCollider.bounds.Contains(transform.position);

        if (currentlyIndoor != isIndoor)
        {
            isIndoor = currentlyIndoor;

            // Apply wind muffling
            if (windFilter != null)
            {
                windFilter.cutoffFrequency = isIndoor ? indoorCutoff : outdoorCutoff;
            }

            Debug.Log(isIndoor ? "Entered building" : "Exited building");
        }
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
        AudioClip[] clips = isIndoor ? indoorSteps : outdoorSteps;
        if (clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        footstepSource.PlayOneShot(clip, Random.Range(0.8f, 1.2f));
    }
}