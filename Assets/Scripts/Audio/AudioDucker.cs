using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AudioDucker : MonoBehaviour
{
    [Header("Audio Source")]
    [Tooltip("The wind/ambient AudioSource you want to duck.")]
    public AudioSource windAudio;

    [Header("The 3 Volume States")]
    [Tooltip("Volume when standing completely outside the cruiser.")]
    [Range(0f, 1f)] public float outsideVolume = 1.0f;

    [Tooltip("Volume when standing inside the cruiser, but the door is completely OPEN. (Base isolation)")]
    [Range(0f, 1f)] public float insideDoorOpenVolume = 0.5f;

    [Tooltip("Volume when standing inside the cruiser, and the door is completely CLOSED. (Full isolation)")]
    [Range(0f, 1f)] public float insideDoorClosedVolume = 0.1f;

    [Header("Transition Speed")]
    [Tooltip("Higher number = Faster volume changes. 3 is a good, natural audio fade.")]
    public float fadeSharpness = 3.0f;

    [Header("Door Integration")]
    [Tooltip("Link the WinchController (Valve) here.")]
    public WinchController linkedDoor;

    [Tooltip("X-axis: Door Open % (0=Closed, 1=Open). Y-axis: Volume Multiplier (0=Sealed, 1=Open Gap).")]
    public AnimationCurve doorOpenCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private bool isPlayerInside = false;
    private float currentTargetVolume;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }

    private void Update()
    {
        if (windAudio == null) return;

        // 1. Figure out what the volume SHOULD be right now
        if (!isPlayerInside)
        {
            // If we are outside, we want full volume, no matter what the door is doing.
            currentTargetVolume = outsideVolume;
        }
        else
        {
            // If we are inside, check the door!
            if (linkedDoor != null)
            {
                // Calculate how open the door is (0.0 is closed, 1.0 is fully open)
                float totalTravel = Mathf.Abs(linkedDoor.openAngle - linkedDoor.closedAngle);
                float currentTravel = Mathf.Abs(linkedDoor.CurrentAngle - linkedDoor.closedAngle);
                float percentOpen = Mathf.Clamp01(currentTravel / totalTravel);

                // Run it through your custom curve to make it feel non-linear
                float curveValue = doorOpenCurve.Evaluate(percentOpen);

                // Find the exact volume between the Open State and Closed State
                currentTargetVolume = Mathf.Lerp(insideDoorClosedVolume, insideDoorOpenVolume, curveValue);
            }
            else
            {
                // If there's no door attached to this script, just give them max isolation
                currentTargetVolume = insideDoorClosedVolume;
            }
        }

        // 2. Smoothly slide the actual audio source towards our target volume
        windAudio.volume = Mathf.Lerp(windAudio.volume, currentTargetVolume, fadeSharpness * Time.deltaTime);
    }
}