using UnityEngine;

// We can safely keep this, as you need at least one collider on the object!
[RequireComponent(typeof(Collider))]
public class AudioDucker : MonoBehaviour
{
    public static AudioDucker CurrentActiveZone = null;

    [Header("Audio Source")]
    public AudioSource windAudio;

    [Header("The 3 Volume States")]
    [Range(0f, 1f)] public float outsideVolume = 1.0f;
    [Range(0f, 1f)] public float insideDoorOpenVolume = 0.5f;
    [Range(0f, 1f)] public float insideDoorClosedVolume = 0.1f;

    [Header("Transition Speed")]
    public float fadeSharpness = 3.0f;

    [Header("Door Integration")]
    public WinchController linkedDoor;
    public AnimationCurve doorOpenCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    // --- THE MULTI-COLLIDER FIX ---
    // Instead of a true/false bool, we count how many trigger zones the player is touching.
    private int playerInsideCount = 0;

    private float currentTargetVolume;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Add 1 to our tracker every time we touch a new collider piece
            playerInsideCount++;
            CurrentActiveZone = this;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Subtract 1 when we leave a piece
            playerInsideCount--;

            // Safety clamp just in case physics get weird
            if (playerInsideCount < 0) playerInsideCount = 0;
        }
    }

    private void Update()
    {
        if (windAudio == null) return;

        if (CurrentActiveZone != this && CurrentActiveZone != null) return;

        // --- NEW LOGIC: If the count is greater than 0, we are inside! ---
        if (playerInsideCount == 0)
        {
            currentTargetVolume = outsideVolume;
        }
        else
        {
            if (linkedDoor != null)
            {
                float totalTravel = Mathf.Abs(linkedDoor.openAngle - linkedDoor.closedAngle);
                float currentTravel = Mathf.Abs(linkedDoor.CurrentAngle - linkedDoor.closedAngle);
                float percentOpen = Mathf.Clamp01(currentTravel / totalTravel);

                float curveValue = doorOpenCurve.Evaluate(percentOpen);
                currentTargetVolume = Mathf.Lerp(insideDoorClosedVolume, insideDoorOpenVolume, curveValue);
            }
            else
            {
                currentTargetVolume = insideDoorClosedVolume;
            }
        }

        windAudio.volume = Mathf.Lerp(windAudio.volume, currentTargetVolume, fadeSharpness * Time.deltaTime);
    }
}