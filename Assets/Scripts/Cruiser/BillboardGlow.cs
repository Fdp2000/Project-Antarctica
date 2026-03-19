using UnityEngine;

public class BillboardGlow : MonoBehaviour
{
    private Transform mainCamTransform;
    private MeshRenderer meshRenderer;
    private Material glowMaterial;

    [Header("Lighthouse Logic")]
    [Tooltip("The Spot Light that is rotating.")]
    public Transform lightTransform;
    [Range(0f, 1f)]
    [Tooltip("How wide the 'beam' of the flare is. 0.9 = very narrow, 0.5 = wide.")]
    public float beamWidth = 0.8f;

    void Start()
    {
        if (Camera.main != null) mainCamTransform = Camera.main.transform;

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null) glowMaterial = meshRenderer.material;
    }

    void LateUpdate()
    {
        if (mainCamTransform == null || lightTransform == null || glowMaterial == null) return;

        // 1. BILLBOARDING: Force the quad to face the player
        transform.LookAt(transform.position + mainCamTransform.rotation * Vector3.forward,
                         mainCamTransform.rotation * Vector3.up);

        // 2. LIGHTHOUSE EFFECT: Calculate if the light is pointing at us
        Vector3 dirToPlayer = (mainCamTransform.position - lightTransform.position).normalized;
        float dotProduct = Vector3.Dot(lightTransform.forward, dirToPlayer);

        // 3. FADING: If the light points at us, dotProduct is 1.0. If away, it's -1.0.
        float visibility = Mathf.Clamp01((dotProduct - beamWidth) / (1f - beamWidth));

        // Apply the visibility to the material color alpha (transparency)
        Color c = glowMaterial.color;
        c.a = visibility;
        glowMaterial.color = c;
    }
}