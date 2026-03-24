using UnityEngine;

public class WindAudioController : MonoBehaviour
{
    public static WindAudioController Instance;

    [Header("Audio")]
    public AudioSource windSource;
    public AudioLowPassFilter lowPass;

    [Header("Volume Settings")]
    public float outdoorVolume = 1f;
    public float indoorVolume = 0.2f;

    [Header("Filter Settings")]
    public float outdoorCutoff = 22000f;
    public float indoorCutoff = 3000f;

    [Header("Blending")]
    [Range(0f, 1f)]
    public float exposure = 1f; // 1 = fully outside, 0 = fully inside
    public float transitionSpeed = 3f;

    private float currentExposure;

    void Awake()
    {
        Instance = this;
        currentExposure = exposure;
    }

    void Update()
    {
        // Smoothly move toward target exposure
        currentExposure = Mathf.Lerp(currentExposure, exposure, Time.deltaTime * transitionSpeed);

        // Blend volume
        if (windSource != null)
        {
            float targetVolume = Mathf.Lerp(indoorVolume, outdoorVolume, currentExposure);
            windSource.volume = targetVolume;
        }

        // Blend filter (muffling)
        if (lowPass != null)
        {
            float targetCutoff = Mathf.Lerp(indoorCutoff, outdoorCutoff, currentExposure);
            lowPass.cutoffFrequency = targetCutoff;
        }
    }

    // 🌬️ Set exposure manually (0 = indoor, 1 = outdoor)
    public void SetExposure(float value)
    {
        exposure = Mathf.Clamp01(value);
    }

    // Optional helpers (nice for readability)
    public void SetIndoor()
    {
        SetExposure(0f);
    }

    public void SetOutdoor()
    {
        SetExposure(1f);
    }

    public void SetSemiOutdoor(float value)
    {
        SetExposure(value); // e.g. 0.3 = mostly inside
    }
}