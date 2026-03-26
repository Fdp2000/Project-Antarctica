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

    [Header("Environment (Skybox)")]
    [Tooltip("Blends the Window -> Rendering -> Lighting -> Intensity Multiplier")]
    public float dayAmbientIntensity = 1.0f;
    public float nightAmbientIntensity = 0.05f;

    [Header("Vehicle Integration")]
    [Tooltip("Drag the Cruiser's front headlights here to automatically turn them on when night falls.")]
    public Light[] vehicleHeadlights;

    private void OnTriggerEnter(Collider other)
    {
        // Safely check if it's the player, OR a vehicle child collider, OR the vehicle parent itself
        bool isPlayer = other.CompareTag("Player");
        bool isVehicle = other.CompareTag("Vehicle") || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Vehicle"));

        if (isPlayer || isVehicle)
        {
            if (triggerOnlyOnce && hasTriggered) return;

            hasTriggered = true;
            Debug.Log("<color=blue>Initiating Day to Night Lighting Transition...</color>");

            // Turn on headlights immediately if the vehicle has them
            if (vehicleHeadlights != null)
            {
                foreach (Light light in vehicleHeadlights)
                {
                    if (light != null) light.enabled = true;
                }
            }

            StartCoroutine(TransitionLighting());
        }
    }

    private IEnumerator TransitionLighting()
    {
        float elapsedTime = 0f;

        // Capture the exact starting values
        Color startSunColor = directionalLight != null ? directionalLight.color : daySunColor;
        float startSunIntensity = directionalLight != null ? directionalLight.intensity : daySunIntensity;
        float startAmbientIntensity = RenderSettings.ambientIntensity;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            // 1. Blend Directional Light
            if (directionalLight != null)
            {
                directionalLight.color = Color.Lerp(startSunColor, nightSunColor, easedT);
                directionalLight.intensity = Mathf.Lerp(startSunIntensity, nightSunIntensity, easedT);
            }

            // 2. Blend Skybox Ambient Intensity
            RenderSettings.ambientIntensity = Mathf.Lerp(startAmbientIntensity, nightAmbientIntensity, easedT);

            yield return null;
        }

        // Hard set final values to ensure absolute precision
        if (directionalLight != null)
        {
            directionalLight.color = nightSunColor;
            directionalLight.intensity = nightSunIntensity;
        }
        RenderSettings.ambientIntensity = nightAmbientIntensity;
    }
}