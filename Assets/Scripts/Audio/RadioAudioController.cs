using UnityEngine;

public class RadioAudioController : MonoBehaviour
{
    [Header("Dependencies")]
    public RadioTuner tuner;

    [Header("Static Channel (Hardware)")]
    public AudioSource staticSource;
    public AudioLowPassFilter staticLowPass;
    [Range(0f, 1f)] public float maxStaticVolume = 1.0f;

    [Tooltip("Static ducks down to 40% volume around 80% signal clarity.")]
    public AnimationCurve staticVolumeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.8f, 0.4f), new Keyframe(1f, 0.4f));
    public AnimationCurve staticCutoffCurve = new AnimationCurve(new Keyframe(0f, 4000f), new Keyframe(1f, 800f));

    [Header("Monster Interference (Driven by Director)")]
    public AudioClip distortedStaticClip;
    public float maxMonsterVolume = 1.0f;
    public float retreatMonsterVolume = 0.4f; // <--- NEW: Dedicated retreat volume
    [Tooltip("How far the static can be heard when the monster is right outside.")]
    public float maxMonsterDistance = 30f;

    [HideInInspector] public bool isMonsterApproaching = false;
    [HideInInspector] public bool isMonsterRetreating = false; // <--- NEW: Retreat toggle
    [HideInInspector] public float approachProgress = 0f;

    private AudioClip normalStaticClip;
    private float normalMaxDistance;

    [Header("Broadcast Channel Components")]
    public AudioSource broadcastSource;
    public AudioLowPassFilter broadcastLowPass;
    public AudioHighPassFilter broadcastHighPass;
    public AudioDistortionFilter broadcastDistortion;
    public AudioChorusFilter broadcastChorus;
    public AudioEchoFilter broadcastEcho;

    void Start()
    {
        // Remember the default settings so we can return to them later
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

        // Ensure we are pointing to the Tuner's updated array winner
        RadioBeacon activeBeacon = tuner.activeBeacon;

        // --- 1. THE STATIC (With Monster Hijack Logic) ---
        if (staticSource != null)
        {
            if (isMonsterApproaching && distortedStaticClip != null)
            {
                // Swap to terrifying static
                if (staticSource.clip != distortedStaticClip)
                {
                    staticSource.clip = distortedStaticClip;
                    staticSource.Play();
                }

                // Lerp volume and distance up as the monster approaches
                float baseVolume = staticVolumeCurve.Evaluate(signal) * maxStaticVolume;

                // --- NEW: Choose which volume ceiling to use ---
                float currentTargetVol = isMonsterRetreating ? retreatMonsterVolume : maxMonsterVolume;

                staticSource.volume = Mathf.Lerp(baseVolume, currentTargetVol, approachProgress);
                staticSource.maxDistance = Mathf.Lerp(normalMaxDistance, maxMonsterDistance, approachProgress);
            }
            else
            {
                // Return to normal static
                if (staticSource.clip != normalStaticClip && normalStaticClip != null)
                {
                    staticSource.clip = normalStaticClip;
                    staticSource.Play();
                }
                staticSource.volume = staticVolumeCurve.Evaluate(signal) * maxStaticVolume;
                staticSource.maxDistance = normalMaxDistance;
            }

            if (staticLowPass != null)
                staticLowPass.cutoffFrequency = staticCutoffCurve.Evaluate(signal);
        }

        // --- 2. NO BEACON FOUND ---
        if (activeBeacon == null)
        {
            if (broadcastSource != null) broadcastSource.volume = 0f;
            return;
        }

        // --- 3. DYNAMIC PAYLOAD LOADING (The Virtual Playhead) ---
        if (broadcastSource != null && broadcastSource.clip != activeBeacon.broadcastPayload)
        {
            broadcastSource.clip = activeBeacon.broadcastPayload;

            if (broadcastSource.clip != null)
            {
                // Instantly sync the audio to the game's run time
                float simulatedLiveTime = Time.time % broadcastSource.clip.length;
                broadcastSource.time = simulatedLiveTime;
            }

            broadcastSource.Play();
        }

        // --- 4. APPLY BEACON INSTRUCTIONS ---
        if (broadcastSource != null)
        {
            // Volume & Stutter
            float currentVolume = activeBeacon.broadcastVolumeCurve.Evaluate(signal) * activeBeacon.maxBroadcastVolume;

            if (activeBeacon.useStutter)
            {
                float stutterThreshold = activeBeacon.signalStutterCurve.Evaluate(signal);
                float noise = Mathf.PerlinNoise(Time.time * activeBeacon.stutterSpeed, 0f);
                if (noise < stutterThreshold) currentVolume = 0f;
            }
            broadcastSource.volume = currentVolume;

            // Pitch & Flutter
            if (activeBeacon.usePitchFlutter)
            {
                float basePitch = activeBeacon.broadcastBasePitchCurve.Evaluate(signal);
                float flutterAmount = activeBeacon.broadcastPitchFlutterCurve.Evaluate(signal);
                float pitchNoise = (Mathf.PerlinNoise(Time.time * activeBeacon.pitchFlutterSpeed, 100f) - 0.5f) * 2f;
                broadcastSource.pitch = basePitch + (pitchNoise * flutterAmount);
            }
            else
            {
                broadcastSource.pitch = 1f;
            }
        }

        // --- 5. APPLY HARDWARE FILTERS ---
        if (broadcastLowPass != null)
            broadcastLowPass.cutoffFrequency = activeBeacon.useLowPass ? activeBeacon.broadcastLowPassCurve.Evaluate(signal) : 22000f;

        if (broadcastHighPass != null)
            broadcastHighPass.cutoffFrequency = activeBeacon.useHighPass ? activeBeacon.broadcastHighPassCurve.Evaluate(signal) : 10f;

        if (broadcastDistortion != null)
            broadcastDistortion.distortionLevel = activeBeacon.useDistortion ? activeBeacon.broadcastDistortionCurve.Evaluate(signal) : 0f;

        if (broadcastChorus != null)
        {
            float chorusMix = activeBeacon.useChorus ? activeBeacon.broadcastChorusMixCurve.Evaluate(signal) : 0f;
            broadcastChorus.wetMix1 = chorusMix;
            broadcastChorus.wetMix2 = chorusMix * 0.5f;
            broadcastChorus.wetMix3 = chorusMix * 0.25f;
        }

        if (broadcastEcho != null)
        {
            broadcastEcho.wetMix = activeBeacon.useEcho ? activeBeacon.echoWetCurve.Evaluate(signal) : 0f;
            broadcastEcho.dryMix = activeBeacon.useEcho ? activeBeacon.echoDryCurve.Evaluate(signal) : 1f;
        }
    }
}