using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AudioBooster : MonoBehaviour
{
    [Header("Audio Source")]
    [Tooltip("The AudioSource you want to fade IN when entering.")]
    public AudioSource targetAudio;

    [Header("The Volume States")]
    [Tooltip("Volume when standing completely outside the zone (Usually 0).")]
    [Range(0f, 1f)] public float outsideVolume = 0.0f;

    [Tooltip("Volume when standing inside, but the door is completely CLOSED. (Muffled)")]
    [Range(0f, 1f)] public float insideDoorClosedVolume = 0.3f;

    [Tooltip("Volume when standing inside, and the door is completely OPEN. (Loudest)")]
    [Range(0f, 1f)] public float insideDoorOpenVolume = 1.0f;

    [Header("Transition Speed")]
    public float fadeSharpness = 3.0f;

    [Header("Door Integration (Optional)")]
    [Tooltip("Leave this empty if there is no door!")]
    public WinchController linkedDoor;
    public AnimationCurve doorOpenCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    // --- MULTI-COLLIDER SUPPORT ---
    private int playerInsideCount = 0;
    private float currentTargetVolume;

    private void Start()
    {
        // Force the audio to start at the correct outside volume
        if (targetAudio != null) targetAudio.volume = outsideVolume;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInsideCount++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInsideCount--;
            if (playerInsideCount < 0) playerInsideCount = 0;
        }
    }

    private void Update()
    {
        if (targetAudio == null) return;

        // If the count is 0, the player has completely left the area
        if (playerInsideCount == 0)
        {
            currentTargetVolume = outsideVolume;
        }
        else
        {
            if (linkedDoor != null)
            {
                // Calculate door open percentage
                float totalTravel = Mathf.Abs(linkedDoor.openAngle - linkedDoor.closedAngle);
                float currentTravel = Mathf.Abs(linkedDoor.CurrentAngle - linkedDoor.closedAngle);
                float percentOpen = Mathf.Clamp01(currentTravel / totalTravel);

                float curveValue = doorOpenCurve.Evaluate(percentOpen);
                currentTargetVolume = Mathf.Lerp(insideDoorClosedVolume, insideDoorOpenVolume, curveValue);
            }
            else
            {
                // If there's no door attached, just give them max volume!
                currentTargetVolume = insideDoorOpenVolume;
            }
        }

        // Smoothly slide the volume up or down
        targetAudio.volume = Mathf.Lerp(targetAudio.volume, currentTargetVolume, fadeSharpness * Time.deltaTime);
    }
}