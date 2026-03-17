using UnityEngine;

[RequireComponent(typeof(Light))]
public class SubtleFlicker : MonoBehaviour
{
    [Header("Flicker Settings")]
    public Light targetLight;

    [Tooltip("The lowest the light will dip.")]
    public float minIntensity = 0.8f;

    [Tooltip("The brightest the light will get.")]
    public float maxIntensity = 1.2f;

    [Tooltip("How fast the voltage fluctuates.")]
    public float flickerSpeed = 3.0f;

    // We use a random offset so if you put this script on 5 different lights, 
    // they don't all flicker at the exact same time!
    private float noiseOffset;

    void Start()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<Light>();
        }

        // Pick a random starting point in the Perlin Noise wave
        noiseOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (targetLight == null) return;

        // Get a smoothly shifting random number between 0.0 and 1.0
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, noiseOffset);

        // Apply that number to the light's intensity
        targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}