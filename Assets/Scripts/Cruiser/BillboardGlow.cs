using UnityEngine;

public class BillboardGlow : MonoBehaviour
{
    private Transform mainCamTransform;
    private MeshRenderer meshRenderer;
    private Material glowMaterial;

    [Header("Lighthouse Logic")]
    public Transform lightTransform;
    [Range(0f, 1f)]
    public float beamWidth = 0.8f;

    [Header("Pro Distance Fading")]
    [Tooltip("The flare is 100% invisible at this distance (meters).")]
    public float minDistance = 1.5f;
    [Tooltip("The flare reaches full brightness at this distance (meters).")]
    public float fullBrightnessDistance = 8.0f;
    [Tooltip("The base brightness/alpha of your sprite.")]
    public float maxAlpha = 1.0f;

    void Start()
    {
        if (Camera.main != null) mainCamTransform = Camera.main.transform;

        meshRenderer = GetComponent<MeshRenderer>();
        // Using material (instantiated) so we don't change the project asset
        if (meshRenderer != null) glowMaterial = meshRenderer.material;
    }

    void LateUpdate()
    {
        if (mainCamTransform == null || lightTransform == null || glowMaterial == null) return;

        // 1. BILLBOARDING: Stay facing the player
        transform.LookAt(transform.position + mainCamTransform.rotation * Vector3.forward,
                         mainCamTransform.rotation * Vector3.up);

        // 2. DISTANCE CALCULATION
        float dist = Vector3.Distance(transform.position, mainCamTransform.position);

        // This creates a 0 to 1 value based on how far away we are
        float distanceAlpha = Mathf.InverseLerp(minDistance, fullBrightnessDistance, dist);

        // 3. LIGHTHOUSE ROTATION LOGIC
        Vector3 dirToPlayer = (mainCamTransform.position - lightTransform.position).normalized;
        float dotProduct = Vector3.Dot(lightTransform.forward, dirToPlayer);

        // Calculate how much the 'beam' hits the eyes
        float visibility = Mathf.Clamp01((dotProduct - beamWidth) / (1f - beamWidth));

        // 4. FINAL ALPHA COMBINATION
        // We multiply the Lighthouse flash by the Distance fade
        Color c = glowMaterial.color;
        c.a = visibility * distanceAlpha * maxAlpha;
        glowMaterial.color = c;
    }
}