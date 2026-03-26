using UnityEngine;
using System.Collections;

public class BlizzardTransition : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Check this if you want the transition to only happen once.")]
    public bool triggerOnlyOnce = true;
    public float transitionDuration = 10.0f;
    private bool hasTriggered = false;

    [Header("Fog Settings (Density Only)")]
    public float dayFogDensity = 0.02f;
    public float nightFogDensity = 0.08f;

    [Header("Snow Particle Settings")]
    public ParticleSystem snowParticleSystem;
    [Tooltip("The light emission rate before the trigger is hit.")]
    public float dayEmissionRate = 500f;
    [Tooltip("The heavy emission rate during the night/blizzard.")]
    public float nightEmissionRate = 2000f;

    private void OnTriggerEnter(Collider other)
    {
        bool isPlayer = other.CompareTag("Player");
        bool isVehicle = other.CompareTag("Vehicle") || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Vehicle"));

        if (isPlayer || isVehicle)
        {
            if (triggerOnlyOnce && hasTriggered) return;

            hasTriggered = true;
            Debug.Log("<color=cyan>Initiating Blizzard Density Transition...</color>");
            StartCoroutine(TransitionWeather());
        }
    }

    private IEnumerator TransitionWeather()
    {
        float elapsedTime = 0f;
        float startFogDensity = RenderSettings.fogDensity;

        // Force the start rate to exactly match our Day variable!
        float startEmissionRate = dayEmissionRate;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, nightFogDensity, easedT);

            if (snowParticleSystem != null)
            {
                var emissionModule = snowParticleSystem.emission;
                // Using the Multiplier is Unity's bulletproof way of updating rates over time at runtime
                emissionModule.rateOverTimeMultiplier = Mathf.Lerp(startEmissionRate, nightEmissionRate, easedT);
            }

            yield return null;
        }

        RenderSettings.fogDensity = nightFogDensity;

        if (snowParticleSystem != null)
        {
            var emissionModule = snowParticleSystem.emission;
            emissionModule.rateOverTimeMultiplier = nightEmissionRate;
        }
    }
}