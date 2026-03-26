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
        if (other.CompareTag("Player"))
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

        // Capture starting values
        float startFogDensity = RenderSettings.fogDensity;

        float startEmissionRate = dayEmissionRate;
        if (snowParticleSystem != null)
        {
            startEmissionRate = snowParticleSystem.emission.rateOverTime.constant;
        }

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            // 1. Blend Fog Density ONLY
            RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, nightFogDensity, easedT);

            // 2. Blend Particle Emission Rate
            if (snowParticleSystem != null)
            {
                var emissionModule = snowParticleSystem.emission;
                // Unity requires assigning a new MinMaxCurve struct when updating rate over time via code
                emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(Mathf.Lerp(startEmissionRate, nightEmissionRate, easedT));
            }

            yield return null;
        }

        // Hard set final values at the end
        RenderSettings.fogDensity = nightFogDensity;

        if (snowParticleSystem != null)
        {
            var emissionModule = snowParticleSystem.emission;
            emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(nightEmissionRate);
        }
    }
}