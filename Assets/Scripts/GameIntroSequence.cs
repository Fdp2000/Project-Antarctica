using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Audio;
using System.Collections;

public class GameIntroSequence : MonoBehaviour
{
    [Header("The Awakening Timers")]
    [Tooltip("How long it takes for the camera to focus and the audio to fade in.")]
    public float introDuration = 5.0f;
    [Tooltip("Wait this many seconds before starting the fade-in.")]
    public float delayBeforeWake = 1.0f;

    [Header("Post Processing (URP)")]
    public Volume globalVolume;
    private DepthOfField depthOfField;

    [Header("Depth of Field Settings (Bokeh)")]
    public float startingFocusDistance = 0.1f;
    public float normalFocusDistance = 10.0f;
    public float startingFocalLength = 150f;
    public float normalFocalLength = 50f;

    [Header("Audio Fade Settings")]
    public AudioMixer mainMixer;
    [Tooltip("This must match exactly what you named the parameter in the top right of the Audio Mixer window.")]
    public string masterVolumeParameter = "MasterVolume";
    [Tooltip("Unity's silent dB level is usually -80")]
    public float silencedVolume = -80f;
    [Tooltip("Unity's default normal dB level is 0")]
    public float normalVolume = 0f;

    void Awake()
    {
        // 1. Instantly force the audio to -80dB (Silence) before frame 1
        if (mainMixer != null)
        {
            mainMixer.SetFloat(masterVolumeParameter, silencedVolume);
        }

        // 2. Setup the camera blur
        if (globalVolume != null && globalVolume.profile.TryGet(out depthOfField))
        {
            depthOfField.active = true;
            depthOfField.focusDistance.Override(startingFocusDistance);
            depthOfField.focalLength.Override(startingFocalLength);
        }
    }

    void Start()
    {
        StartCoroutine(WakeUpRoutine());
    }

    private IEnumerator WakeUpRoutine()
    {
        if (delayBeforeWake > 0f)
        {
            yield return new WaitForSeconds(delayBeforeWake);
        }

        Debug.Log("<color=cyan>INTRO: Audio and Camera fading in...</color>");
        float elapsed = 0f;

        while (elapsed < introDuration)
        {
            elapsed += Time.deltaTime;

            // Linear time (0 to 1)
            float t = elapsed / introDuration;

            // Ken Perlin's SmootherStep for the camera blur
            float easedT = t * t * t * (t * (6f * t - 15f) + 10f);

            // 1. Smoothly focus the camera
            if (depthOfField != null)
            {
                depthOfField.focusDistance.Override(Mathf.Lerp(startingFocusDistance, normalFocusDistance, easedT));
                depthOfField.focalLength.Override(Mathf.Lerp(startingFocalLength, normalFocalLength, easedT));
            }

            // 2. Logarithmic Audio Fade
            // Because decibels aren't linear, lerping a float from 0.0001 to 1 and converting to Log10 makes a perfect audio swell
            if (mainMixer != null)
            {
                float linearVolume = Mathf.Lerp(0.0001f, 1f, t);
                float currentDecibels = Mathf.Log10(linearVolume) * 20f;

                // Clamp it just in case, to ensure it stays between -80 and 0
                currentDecibels = Mathf.Clamp(currentDecibels, silencedVolume, normalVolume);
                mainMixer.SetFloat(masterVolumeParameter, currentDecibels);
            }

            yield return null;
        }

        // Hard set final values
        if (mainMixer != null) mainMixer.SetFloat(masterVolumeParameter, normalVolume);

        if (depthOfField != null)
        {
            depthOfField.focusDistance.Override(normalFocusDistance);
            depthOfField.focalLength.Override(normalFocalLength);
            depthOfField.active = false;
        }

        Debug.Log("<color=green>INTRO: Sequence Complete.</color>");
    }
}