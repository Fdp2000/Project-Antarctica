using UnityEngine;
using System.Collections;

public class RadioBeacon : MonoBehaviour
{
    [Header("Broadcast Details (WebGL Crossfade)")]
    public float broadcastFrequency = 155.0f;
    [Tooltip("The normal, clean audio from Audacity.")]
    public AudioClip cleanPayload;
    [Tooltip("The heavily distorted/filtered audio from Audacity.")]
    public AudioClip distortedPayload;
    [Range(0f, 1f)] public float maxBroadcastVolume = 1.0f;

    [Header("Narrative Progression")]
    [Tooltip("Checked automatically when the CRT punchcard is collected.")]
    public bool isCompleted = false;
    [Tooltip("True when the signal has completely faded to 0.")]
    public bool isDead = false;
    [HideInInspector] public bool isFadingOut = false;

    [Header("Visuals")]
    public Material uniqueTapeMaterial;
    public Transform playerVehicle;

    // --- MERGED RADIUS ---
    [Tooltip("The radius where the signal is 100% strong, AND the boundary that triggers the fade-out when leaving.")]
    public float proximityRadius = 50f;
    [Tooltip("How many seconds it takes to Lerp the signal from 100% to 0%.")]
    public float fadeLerpDuration = 6.0f;
    [HideInInspector] public float signalMultiplier = 1.0f;

    [Header("Volume & Crossfade Curves")]
    [Tooltip("Volume of the CLEAN track. X = Signal (0-1), Y = Volume (0-1). Usually peaks at 1.0 signal.")]
    public AnimationCurve cleanVolumeCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.6f, 0.1f), new Keyframe(1f, 1f));

    [Tooltip("Volume of the DISTORTED track. Peaks when signal is weak/medium, fades out when signal is 1.0 or 0.0.")]
    public AnimationCurve distortedVolumeCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.2f, 1f), new Keyframe(0.8f, 0.4f), new Keyframe(1f, 0f));

    [Header("Stutter Effect")]
    public bool useStutter = false;
    public float stutterSpeed = 10f;
    public AnimationCurve signalStutterCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));

    [Header("Pitch & Flutter")]
    public bool usePitchFlutter = false;
    public float pitchFlutterSpeed = 5f;
    public AnimationCurve broadcastBasePitchCurve = new AnimationCurve(new Keyframe(0f, 0.8f), new Keyframe(1f, 1f));
    public AnimationCurve broadcastPitchFlutterCurve = new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(1f, 0f));

    void Update()
    {
        if (isCompleted && !isFadingOut && !isDead && playerVehicle != null)
        {
            float dist = Vector3.Distance(transform.position, playerVehicle.position);
            if (dist > proximityRadius)
            {
                StartCoroutine(KillSignalSequence());
            }
        }
    }

    private IEnumerator KillSignalSequence()
    {
        isFadingOut = true;
        float elapsedTime = 0f;
        while (elapsedTime < fadeLerpDuration)
        {
            elapsedTime += Time.deltaTime;
            signalMultiplier = Mathf.Lerp(1.0f, 0.0f, elapsedTime / fadeLerpDuration);
            yield return null;
        }
        signalMultiplier = 0f;
        isDead = true;
        isFadingOut = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, proximityRadius);
        if (playerVehicle != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, playerVehicle.position);
        }
    }
}