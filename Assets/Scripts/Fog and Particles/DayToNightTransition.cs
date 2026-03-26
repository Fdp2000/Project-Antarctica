using UnityEngine;
using System.Collections;

public class DayToNightTransition : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Check this if you want the transition to only happen once.")]
    public bool triggerOnlyOnce = true;
    public float transitionDuration = 10.0f;
    private bool hasTriggered = false;

    [Header("Directional Light (Sun/Moon)")]
    public Light directionalLight;
    public Color daySunColor = Color.white;
    public Color nightSunColor = new Color(0.2f, 0.4f, 0.8f); // Pale blue moonlight
    public float daySunIntensity = 1.0f;
    public float nightSunIntensity = 0.1f;

    [Header("Environment (Ambient Light)")]
    [Tooltip("NOTE: Window -> Rendering -> Lighting -> Environment Lighting Source MUST be set to 'Color' for this to work!")]
    public Color dayAmbientColor = new Color(0.6f, 0.6f, 0.6f);
    public Color nightAmbientColor = new Color(0.05f, 0.05f, 0.1f); // Almost pitch black blue

    [Header("Fog Settings")]
    public Color dayFogColor = Color.white;
    public Color nightFogColor = new Color(0.1f, 0.15f, 0.25f);
    public float dayFogDensity = 0.02f;
    public float nightFogDensity = 0.05f; // Thicker fog at night

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is the Player
        if (other.CompareTag("Player"))
        {
            if (triggerOnlyOnce && hasTriggered) return;

            hasTriggered = true;
            Debug.Log("<color=blue>Initiating Day to Night Transition...</color>");
            StartCoroutine(TransitionLighting());
        }
    }

    private IEnumerator TransitionLighting()
    {
        float elapsedTime = 0f;

        // Capture the exact starting values just in case they aren't exactly the "Day" values
        Color startSunColor = directionalLight.color;
        float startSunIntensity = directionalLight.intensity;
        Color startAmbientColor = RenderSettings.ambientLight;
        Color startFogColor = RenderSettings.fogColor;
        float startFogDensity = RenderSettings.fogDensity;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;

            // Smoothly ease the transition so it doesn't abruptly start or stop
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            // 1. Blend Directional Light
            if (directionalLight != null)
            {
                directionalLight.color = Color.Lerp(startSunColor, nightSunColor, easedT);
                directionalLight.intensity = Mathf.Lerp(startSunIntensity, nightSunIntensity, easedT);
            }

            // 2. Blend Ambient Light
            RenderSettings.ambientLight = Color.Lerp(startAmbientColor, nightAmbientColor, easedT);

            // 3. Blend Fog
            RenderSettings.fogColor = Color.Lerp(startFogColor, nightFogColor, easedT);
            RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, nightFogDensity, easedT);

            yield return null;
        }

        // Hard set final values to ensure absolute precision at the end of the timer
        if (directionalLight != null)
        {
            directionalLight.color = nightSunColor;
            directionalLight.intensity = nightSunIntensity;
        }
        RenderSettings.ambientLight = nightAmbientColor;
        RenderSettings.fogColor = nightFogColor;
        RenderSettings.fogDensity = nightFogDensity;
    }
}