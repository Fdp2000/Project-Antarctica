using UnityEngine;

public class RadioAudioController : MonoBehaviour
{
    [Header("Dependencies")]
    public RadioTuner tuner;

    [Header("Static Channel (Hardware)")]
    public AudioSource staticSource;
    [Range(0f, 1f)] public float maxStaticVolume = 1.0f;
    public AnimationCurve staticVolumeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.8f, 0.4f), new Keyframe(1f, 0.4f));

    [Header("Monster Interference (Driven by Director)")]
    public AudioClip distortedStaticClip;
    public float approachVolume = 1.0f;
    public float retreatVolume = 0.4f;
    public float maxMonsterDistance = 30f;
    [HideInInspector] public bool isMonsterApproaching = false;
    [HideInInspector] public bool isMonsterRetreating = false;
    [HideInInspector] public float approachProgress = 0f;

    private AudioClip normalStaticClip;
    private float normalMaxDistance;

    [Header("Broadcast Channel Components (WebGL)")]
    [Tooltip("Plays the clean, untouched audio from the beacon.")]
    public AudioSource cleanBroadcastSource;
    [Tooltip("Plays the pre-baked, distorted audio from Audacity.")]
    public AudioSource distortedBroadcastSource;

    void Start()
    {
        if (staticSource != null)
        {
            normalStaticClip = staticSource.clip;
            normalMaxDistance = staticSource.maxDistance;
        }
    }

    void Update()
    {
        if (tuner == null) return;

        float signal = tuner.finalSignalClarity;
        RadioBeacon activeBeacon = tuner.activeBeacon;

        // --- 1. THE STATIC (With Monster Hijack Logic) ---
        if (staticSource != null)
        {
            if (isMonsterApproaching && distortedStaticClip != null)
            {
                if (staticSource.clip != distortedStaticClip)
                {
                    staticSource.clip = distortedStaticClip;
                    staticSource.Play();
                }

                float baseVolume = staticVolumeCurve.Evaluate(signal) * maxStaticVolume;
                if (isMonsterRetreating) staticSource.volume = Mathf.Lerp(baseVolume, retreatVolume, approachProgress);
                else staticSource.volume = Mathf.Lerp(baseVolume, approachVolume, approachProgress);

                staticSource.maxDistance = Mathf.Lerp(normalMaxDistance, maxMonsterDistance, approachProgress);
            }
            else
            {
                if (staticSource.clip != normalStaticClip && normalStaticClip != null) staticSource.clip = normalStaticClip;
                if (!staticSource.isPlaying && staticSource.clip != null) staticSource.Play();

                staticSource.volume = staticVolumeCurve.Evaluate(signal) * maxStaticVolume;
                staticSource.maxDistance = normalMaxDistance;
            }
        }

        // --- 2. NO BEACON FOUND ---
        if (activeBeacon == null)
        {
            if (cleanBroadcastSource != null) cleanBroadcastSource.volume = 0f;
            if (distortedBroadcastSource != null) distortedBroadcastSource.volume = 0f;
            return;
        }

        // --- 3. DYNAMIC PAYLOAD LOADING (The Virtual Playhead) ---
        if (cleanBroadcastSource != null && distortedBroadcastSource != null && cleanBroadcastSource.clip != activeBeacon.cleanPayload)
        {
            cleanBroadcastSource.clip = activeBeacon.cleanPayload;
            distortedBroadcastSource.clip = activeBeacon.distortedPayload;

            if (cleanBroadcastSource.clip != null)
            {
                // Sync the playheads perfectly
                float simulatedLiveTime = Time.time % cleanBroadcastSource.clip.length;
                cleanBroadcastSource.time = simulatedLiveTime;

                if (distortedBroadcastSource.clip != null)
                {
                    // Ensure the distorted file is exactly the same length in Audacity, or this math shifts slightly!
                    distortedBroadcastSource.time = simulatedLiveTime;
                }
            }

            cleanBroadcastSource.Play();
            distortedBroadcastSource.Play();
        }

        // --- 4. APPLY BEACON INSTRUCTIONS (The Crossfade) ---
        if (cleanBroadcastSource != null && distortedBroadcastSource != null)
        {
            // Evaluate both curves
            float cleanVol = activeBeacon.cleanVolumeCurve.Evaluate(signal) * activeBeacon.maxBroadcastVolume;
            float distVol = activeBeacon.distortedVolumeCurve.Evaluate(signal) * activeBeacon.maxBroadcastVolume;

            // Apply Stutter (cuts out both tracks)
            if (activeBeacon.useStutter)
            {
                float stutterThreshold = activeBeacon.signalStutterCurve.Evaluate(signal);
                float noise = Mathf.PerlinNoise(Time.time * activeBeacon.stutterSpeed, 0f);
                if (noise < stutterThreshold)
                {
                    cleanVol = 0f;
                    distVol = 0f;
                }
            }

            cleanBroadcastSource.volume = cleanVol;
            distortedBroadcastSource.volume = distVol;

            // Apply Pitch Flutter (WebGL supports Pitch shifting natively!)
            if (activeBeacon.usePitchFlutter)
            {
                float basePitch = activeBeacon.broadcastBasePitchCurve.Evaluate(signal);
                float flutterAmount = activeBeacon.broadcastPitchFlutterCurve.Evaluate(signal);
                float pitchNoise = (Mathf.PerlinNoise(Time.time * activeBeacon.pitchFlutterSpeed, 100f) - 0.5f) * 2f;

                float finalPitch = basePitch + (pitchNoise * flutterAmount);
                cleanBroadcastSource.pitch = finalPitch;
                distortedBroadcastSource.pitch = finalPitch;
            }
            else
            {
                cleanBroadcastSource.pitch = 1f;
                distortedBroadcastSource.pitch = 1f;
            }
        }
    }
}