using UnityEngine;
using System.Collections;

public class RadioBeacon : MonoBehaviour
{
    [Header("Broadcast Details")]
    public float broadcastFrequency = 155.0f;
    public AudioClip broadcastPayload;
    [Range(0f, 1f)] public float maxBroadcastVolume = 1.0f;

    [Header("Narrative Progression")]
    [Tooltip("Checked automatically when the CRT punchcard is collected.")]
    public bool isCompleted = false;

    [Tooltip("True when the signal has completely faded to 0.")]
    public bool isDead = false;

    [HideInInspector] public bool isFadingOut = false;

    [Header("Visuals")]
    [Tooltip("The specific texture/material for this POI's cassette tape.")]
    public Material uniqueTapeMaterial;

    [Tooltip("Drag the player's vehicle here to track distance.")]
    public Transform playerVehicle;

    // --- MERGED RADIUS ---
    [Tooltip("The radius where the signal is 100% strong, AND the boundary that triggers the fade-out when leaving.")]
    public float proximityRadius = 50f;

    [Tooltip("How many seconds it takes to Lerp the signal from 100% to 0%.")]
    public float fadeLerpDuration = 6.0f;

    // The Tuner reads this to dynamically drop the math to 0
    [HideInInspector] public float signalMultiplier = 1.0f;

    [Header("Volume & Stutter")]
    public AnimationCurve broadcastVolumeCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
    public bool useStutter = false;
    public float stutterSpeed = 10f;
    public AnimationCurve signalStutterCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));

    [Header("Pitch & Flutter")]
    public bool usePitchFlutter = false;
    public float pitchFlutterSpeed = 5f;
    public AnimationCurve broadcastBasePitchCurve = new AnimationCurve(new Keyframe(0f, 0.8f), new Keyframe(1f, 1f));
    public AnimationCurve broadcastPitchFlutterCurve = new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(1f, 0f));

    [Header("Filters & Distortion")]
    public bool useLowPass = true;
    public AnimationCurve broadcastLowPassCurve = new AnimationCurve(new Keyframe(0f, 1000f), new Keyframe(1f, 22000f));

    public bool useHighPass = false;
    public AnimationCurve broadcastHighPassCurve = new AnimationCurve(new Keyframe(0f, 500f), new Keyframe(1f, 10f));

    public bool useDistortion = true;
    public AnimationCurve broadcastDistortionCurve = new AnimationCurve(new Keyframe(0f, 0.8f), new Keyframe(1f, 0f));

    [Header("Spatial FX")]
    public bool useChorus = false;
    public AnimationCurve broadcastChorusMixCurve = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 0f));

    public bool useEcho = false;
    public AnimationCurve echoWetCurve = new AnimationCurve(new Keyframe(0f, 0.8f), new Keyframe(1f, 0f));
    public AnimationCurve echoDryCurve = new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(1f, 1f));

    void Update()
    {
        // Check if we should start the death sequence using the unified proximityRadius
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
            // Smoothly crush the multiplier down to 0
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