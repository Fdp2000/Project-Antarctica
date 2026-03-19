using UnityEngine;

public class FootstepAudio : MonoBehaviour
{
    public AudioSource footstepSource;
    public AudioClip[] indoorSteps;
    public AudioClip[] outdoorSteps;

    public float stepInterval = 0.4f; // time between footstep sounds

    private bool isIndoor = false;
    private float stepTimer = 0f;

    void Update()
    {
        // Check if any movement key is pressed
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
            stepTimer = 0f; // reset timer when stopped
        }
    }

    void PlayStep()
    {
        AudioClip[] clips = isIndoor ? indoorSteps : outdoorSteps;
        if (clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        footstepSource.PlayOneShot(clip, Random.Range(0.8f, 1.2f));
    }

    public void SetIndoor(bool indoor)
    {
        isIndoor = indoor;
    }
}