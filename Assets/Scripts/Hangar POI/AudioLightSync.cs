using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioLightSync : MonoBehaviour
{
    [Header("Light Settings")]
    public Light deskLamp;
    public float activeIntensity = 5.0f;
    public float idleIntensity = 0.5f;

    [Header("Audio Settings")]
    [Tooltip("How loud the audio needs to be to trigger the light (0.0 to 1.0)")]
    public float volumeThreshold = 0.05f;

    private AudioSource audioSource;
    // An array to hold a tiny snapshot of the audio data
    private float[] samples = new float[256];

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // If the audio isn't playing, ensure the light is idle
        if (!audioSource.isPlaying)
        {
            if (deskLamp != null) deskLamp.intensity = idleIntensity;
            return;
        }

        // 1. Grab a tiny snapshot of the currently playing audio
        audioSource.GetOutputData(samples, 0);

        // 2. Find the loudest peak in this snapshot
        float currentLoudness = 0f;
        foreach (float sample in samples)
        {
            float absoluteSample = Mathf.Abs(sample);
            if (absoluteSample > currentLoudness)
            {
                currentLoudness = absoluteSample;
            }
        }

        // 3. If the loudness hits our threshold (a beep!), flash the light
        if (deskLamp != null)
        {
            if (currentLoudness > volumeThreshold)
            {
                deskLamp.intensity = activeIntensity;
            }
            else
            {
                deskLamp.intensity = idleIntensity;
            }
        }
    }
}