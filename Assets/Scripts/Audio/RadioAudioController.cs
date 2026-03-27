using UnityEngine;

public class RadioAudioController : MonoBehaviour
{
    [Header("Dependencies")]
    public RadioTuner tuner;

    [Header("Static Channel (Hardware)")]
    public AudioSource staticSource;
    [Range(0f, 1f)] public float maxStaticVolume = 1.0f;
    public AnimationCurve staticVolumeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.8f, 0.4f), new Keyframe(1f, 0.4f));

    [Header("Monster Interference (General)")]
    public AudioClip distortedStaticClip;
    [Tooltip("The massive 3D radius the static expands to when the monster is at the door.")]
    public float maxMonsterDistance = 30f;
    [HideInInspector] public bool isMonsterApproaching = false;
    [HideInInspector] public bool isMonsterRetreating = false;
    [HideInInspector] public float approachProgress = 0f;

    [Header("Monster Interference (Toggles)")]
    public bool useMonsterPitchWarping = true;
    public bool useMonsterDucking = true;
    public bool useMonsterDistanceExpansion = true; // <--- NEW TOGGLE

    [Header("Monster Interference (APPROACH Curves)")]
    [Tooltip("Multiplier for static volume. X=0 (Far), X=1 (At the door).")]
    public AnimationCurve approachStaticVolumeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.8f, 1.2f), new Keyframe(1f, 2.5f));
    [Tooltip("Bends the pitch of ALL radio audio. Drop to 0.7 at X=1 for a demonic slowdown.")]
    public AnimationCurve approachPitchCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.7f));
    [Tooltip("Ducks the actual broadcast station so static takes over. Y=1 is normal, Y=0 is muted.")]
    public AnimationCurve approachDuckingCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 0.5f), new Keyframe(1f, 0f));
    [Tooltip("Expands the 3D radius of the static. Y=0 is normal radius, Y=1 is Max Monster Distance.")]
    public AnimationCurve approachDistanceCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.8f, 0.2f), new Keyframe(1f, 1f)); // <--- NEW CURVE

    [Header("Monster Interference (RETREAT Curves)")]
    [Tooltip("Note: During retreat, X goes from 1.0 (At the door) down to 0.0 (Far away).")]
    public AnimationCurve retreatStaticVolumeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.9f, 1f), new Keyframe(1f, 2.5f));
    public AnimationCurve retreatPitchCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.7f));
    public AnimationCurve retreatDuckingCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
    public AnimationCurve retreatDistanceCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f)); // <--- NEW CURVE

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
            normalMaxDistance = staticSource.maxDistance; // Memorizes your standard 5.8 radius
        }
    }

    void Update()
    {
        if (tuner == null) return;

        float signal = tuner.finalSignalClarity;
        RadioBeacon activeBeacon = tuner.activeBeacon;

        // --- 1. THE STATIC (With Advanced Split Monster Logic) ---
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

                // Volume
                float monsterVolMultiplier = isMonsterRetreating ? retreatStaticVolumeCurve.Evaluate(approachProgress) : approachStaticVolumeCurve.Evaluate(approachProgress);
                staticSource.volume = baseVolume * monsterVolMultiplier;

                // --- NEW: 3D Distance Expansion via Curve ---
                if (useMonsterDistanceExpansion)
                {
                    float distanceBlend = isMonsterRetreating ? retreatDistanceCurve.Evaluate(approachProgress) : approachDistanceCurve.Evaluate(approachProgress);
                    staticSource.maxDistance = Mathf.Lerp(normalMaxDistance, maxMonsterDistance, distanceBlend);
                }
                else
                {
                    staticSource.maxDistance = normalMaxDistance;
                }

                // Pitch
                if (useMonsterPitchWarping)
                {
                    staticSource.pitch = isMonsterRetreating ? retreatPitchCurve.Evaluate(approachProgress) : approachPitchCurve.Evaluate(approachProgress);
                }
                else
                {
                    staticSource.pitch = 1f;
                }
            }
            else
            {
                if (staticSource.clip != normalStaticClip && normalStaticClip != null) staticSource.clip = normalStaticClip;
                if (!staticSource.isPlaying && staticSource.clip != null) staticSource.Play();

                staticSource.volume = staticVolumeCurve.Evaluate(signal) * maxStaticVolume;
                staticSource.maxDistance = normalMaxDistance;
                staticSource.pitch = 1f;
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
                float simulatedLiveTime = Time.time % cleanBroadcastSource.clip.length;
                cleanBroadcastSource.time = simulatedLiveTime;

                if (distortedBroadcastSource.clip != null)
                {
                    distortedBroadcastSource.time = simulatedLiveTime;
                }
            }

            cleanBroadcastSource.Play();
            distortedBroadcastSource.Play();
        }

        // --- 4. APPLY BEACON INSTRUCTIONS (The Crossfade) ---
        if (cleanBroadcastSource != null && distortedBroadcastSource != null)
        {
            float cleanVol = activeBeacon.cleanVolumeCurve.Evaluate(signal) * activeBeacon.maxBroadcastVolume;
            float distVol = activeBeacon.distortedVolumeCurve.Evaluate(signal) * activeBeacon.maxBroadcastVolume;

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

            // --- MONSTER BROADCAST DUCKING ---
            if (isMonsterApproaching && useMonsterDucking)
            {
                float duckingMultiplier = isMonsterRetreating ? retreatDuckingCurve.Evaluate(approachProgress) : approachDuckingCurve.Evaluate(approachProgress);
                cleanVol *= duckingMultiplier;
                distVol *= duckingMultiplier;
            }

            cleanBroadcastSource.volume = cleanVol;
            distortedBroadcastSource.volume = distVol;

            // --- PITCH FLUTTER & WARPING ---
            float finalPitch = 1f;
            if (activeBeacon.usePitchFlutter)
            {
                float basePitch = activeBeacon.broadcastBasePitchCurve.Evaluate(signal);
                float flutterAmount = activeBeacon.broadcastPitchFlutterCurve.Evaluate(signal);
                float pitchNoise = (Mathf.PerlinNoise(Time.time * activeBeacon.pitchFlutterSpeed, 100f) - 0.5f) * 2f;
                finalPitch = basePitch + (pitchNoise * flutterAmount);
            }

            if (isMonsterApproaching && useMonsterPitchWarping)
            {
                float monsterPitchModifier = isMonsterRetreating ? retreatPitchCurve.Evaluate(approachProgress) : approachPitchCurve.Evaluate(approachProgress);
                finalPitch *= monsterPitchModifier;
            }

            cleanBroadcastSource.pitch = finalPitch;
            distortedBroadcastSource.pitch = finalPitch;
        }
    }
}