using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioPitchFlutter : MonoBehaviour
{
    [Header("Pitch Settings")]
    [Tooltip("The normal, resting pitch of the audio (usually 1.0).")]
    public float basePitch = 1.0f;

    [Tooltip("How far the pitch bends. (e.g., 0.2 means it wobbles between 0.8 and 1.2).")]
    public float flutterAmount = 0.2f;

    [Tooltip("How incredibly fast the pitch vibrates/wobbles.")]
    public float flutterSpeed = 8.0f;

    [Header("Vibe / Style")]
    [Tooltip("If TRUE: Erratic, unpredictable jitter (Broken tape player / Monster).\nIf FALSE: Smooth, rhythmic back-and-forth (Siren / Mechanical).")]
    public bool chaoticFlutter = true;

    private AudioSource audioSource;
    private float uniqueTimeOffset;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // This ensures that if you put this script on 5 different objects, 
        // they don't all wobble in perfect synchronization!
        uniqueTimeOffset = Random.Range(0f, 1000f);
    }

    void Update()
    {
        // Save performance by not doing math if the sound is muted or stopped
        if (!audioSource.isPlaying || audioSource.volume <= 0.01f) return;

        float currentOffset = 0f;

        if (chaoticFlutter)
        {
            // --- THE BROKEN TAPE METHOD (Perlin Noise) ---
            // Generates a smooth but random number between 0 and 1
            float noise = Mathf.PerlinNoise((Time.time + uniqueTimeOffset) * flutterSpeed, 0f);

            // Shift the 0 to 1 range into a -1 to +1 range, then multiply by your amount
            currentOffset = (noise - 0.5f) * 2f * flutterAmount;
        }
        else
        {
            // --- THE MECHANICAL METHOD (Sine Wave) ---
            // A perfect mathematical wave bouncing from -1 to 1
            currentOffset = Mathf.Sin((Time.time + uniqueTimeOffset) * flutterSpeed) * flutterAmount;
        }

        // Apply it directly to the AudioSource
        audioSource.pitch = basePitch + currentOffset;
    }
}