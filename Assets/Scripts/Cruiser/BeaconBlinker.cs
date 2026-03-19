using UnityEngine;

public class BeaconBlinker : MonoBehaviour
{
    [Header("Light Settings")]
    public Light beaconLight;
    public MeshRenderer bulbRenderer;
    public Material onMaterial;
    public Material offMaterial;

    [Header("Pulse Settings")]
    public float minIntensity = 0.2f;
    public float maxIntensity = 2.5f;
    public float pulseSpeed = 2.0f;

    private void Update()
    {
        if (beaconLight == null) return;

        // Create a smooth 0 to 1 value using a Sine wave
        float lerpWeight = (Mathf.Sin(Time.time * pulseSpeed) + 1.0f) / 2.0f;

        // Apply the intensity
        beaconLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, lerpWeight);

        // Optional: Also lerp the emission color of the bulb for a better look
        if (bulbRenderer != null && onMaterial != null && offMaterial != null)
        {
            bulbRenderer.material.Lerp(offMaterial, onMaterial, lerpWeight);
        }
    }
}