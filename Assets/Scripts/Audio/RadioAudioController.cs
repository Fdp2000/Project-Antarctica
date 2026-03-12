using UnityEngine;

public class RadioAudioController : MonoBehaviour
{
    [Header("Dependencies")]
    public RadioTuner tuner;

    [Header("Static Channel")]
    public AudioSource staticSource;
    public AudioLowPassFilter staticLowPass;
    [Range(0f, 1f)] public float maxStaticVolume = 1.0f;

    public AnimationCurve staticVolumeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.4f));
    public AnimationCurve staticCutoffCurve = new AnimationCurve(new Keyframe(0f, 4000f), new Keyframe(1f, 800f));

    [Header("Broadcast Channel Components")]
    public AudioSource broadcastSource;
    public AudioLowPassFilter broadcastLowPass;
    public AudioHighPassFilter broadcastHighPass;
    public AudioDistortionFilter broadcastDistortion;
    public AudioChorusFilter broadcastChorus;
    public AudioEchoFilter broadcastEcho;

    [Header("Broadcast Mix Settings")]
    [Range(0f, 1f)] public float maxBroadcastVolume = 0.5f;
    public float pitchFlutterSpeed = 15f;
    public float stutterSpeed = 30f;

    [Header("1. Volume & Stutter Curves")]
    [Tooltip("Keep flat if you want to navigate by clarity, or curve it to navigate by volume.")]
    public AnimationCurve broadcastVolumeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
    [Tooltip("Violent audio dropouts. 0.8 heavy stutter dropping to 0.0 stable connection.")]
    public AnimationCurve signalStutterCurve = new AnimationCurve(new Keyframe(0f, 0.8f), new Keyframe(0.35f, 0f));

    [Header("2. Frequency Curves (EQ)")]
    public AnimationCurve broadcastLowPassCurve = new AnimationCurve(new Keyframe(0f, 250f), new Keyframe(1f, 4000f));
    public AnimationCurve broadcastHighPassCurve = new AnimationCurve(new Keyframe(0f, 400f), new Keyframe(1f, 300f));

    [Header("3. Texture Curves (Grit & Pitch)")]
    public AnimationCurve broadcastDistortionCurve = new AnimationCurve(new Keyframe(0f, 0.8f), new Keyframe(1f, 0f));
    public AnimationCurve broadcastBasePitchCurve = new AnimationCurve(new Keyframe(0f, 0.85f), new Keyframe(1f, 1f));
    public AnimationCurve broadcastPitchFlutterCurve = new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(1f, 0f));

    [Header("4. Atmosphere Curves (Space & Smear)")]
    [Tooltip("The swirling phaser effect.")]
    public AnimationCurve broadcastChorusMixCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));

    [Tooltip("Echo smear. 1.0 heavily smeared drone dropping to 0.0 clean rhythm.")]
    public AnimationCurve echoWetCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.35f, 0f));
    [Tooltip("Echo original sound. 0.1 muffled original rising to 1.0 clean original.")]
    public AnimationCurve echoDryCurve = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(0.35f, 1f));

    void Update()
    {
        if (tuner == null) return;

        float signal = tuner.finalSignalClarity;

        // --- 0. Dynamic Payload Loading ---
        if (tuner.targetBeacon != null && broadcastSource != null)
        {
            if (broadcastSource.clip != tuner.targetBeacon.broadcastPayload)
            {
                broadcastSource.clip = tuner.targetBeacon.broadcastPayload;
                broadcastSource.Play();
            }
        }

        // --- 1. THE STATIC ---
        if (staticSource != null)
            staticSource.volume = staticVolumeCurve.Evaluate(signal) * maxStaticVolume;
        if (staticLowPass != null)
            staticLowPass.cutoffFrequency = staticCutoffCurve.Evaluate(signal);

        // --- 2. THE BROADCAST AUDIO ---
        if (broadcastSource != null)
        {
            // Volume & Stutter
            float currentVolume = broadcastVolumeCurve.Evaluate(signal) * maxBroadcastVolume;
            float stutterThreshold = signalStutterCurve.Evaluate(signal);
            float noise = Mathf.PerlinNoise(Time.time * stutterSpeed, 0f);
            if (noise < stutterThreshold) currentVolume = 0f;

            broadcastSource.volume = currentVolume;

            // Pitch & Flutter
            float basePitch = broadcastBasePitchCurve.Evaluate(signal);
            float flutterAmount = broadcastPitchFlutterCurve.Evaluate(signal);
            float pitchNoise = (Mathf.PerlinNoise(Time.time * pitchFlutterSpeed, 100f) - 0.5f) * 2f;
            broadcastSource.pitch = basePitch + (pitchNoise * flutterAmount);
        }

        // --- 3. THE HARDWARE FILTERS ---
        if (broadcastLowPass != null)
            broadcastLowPass.cutoffFrequency = broadcastLowPassCurve.Evaluate(signal);

        if (broadcastHighPass != null)
            broadcastHighPass.cutoffFrequency = broadcastHighPassCurve.Evaluate(signal);

        if (broadcastDistortion != null)
            broadcastDistortion.distortionLevel = broadcastDistortionCurve.Evaluate(signal);

        if (broadcastChorus != null)
        {
            float chorusMix = broadcastChorusMixCurve.Evaluate(signal);
            broadcastChorus.wetMix1 = chorusMix;
            broadcastChorus.wetMix2 = chorusMix * 0.5f;
            broadcastChorus.wetMix3 = chorusMix * 0.25f;
        }

        if (broadcastEcho != null)
        {
            broadcastEcho.wetMix = echoWetCurve.Evaluate(signal);
            broadcastEcho.dryMix = echoDryCurve.Evaluate(signal);
        }
    }
}