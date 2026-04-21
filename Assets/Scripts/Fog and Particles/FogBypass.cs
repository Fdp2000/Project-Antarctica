using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class POISettings
{
    public string locationName = "New POI";
    public Transform targetTransform;
    public float clearDistance = 10f;
    public float fullFogDistance = 15.9f;
}

public class FogBypass : MonoBehaviour
{
    [Tooltip("Drag your Dashboard Overlay Camera here")]
    public Camera overlayCamera;

    [Header("Points of Interest")]
    [Tooltip("Add the Cruiser, Hangar, Ice Cave, etc. here!")]
    public POISettings[] locations;

    private bool defaultFogState;
    private float defaultFogDensity;

    private POISettings closestPOI;
    private float distanceToClosest;

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        RenderPipelineManager.endCameraRendering += OnEndCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        RenderPipelineManager.endCameraRendering -= OnEndCamera;
    }

    private void Update()
    {
        if (overlayCamera == null || locations.Length == 0) return;

        closestPOI = null;
        distanceToClosest = float.MaxValue;

        foreach (var poi in locations)
        {
            if (poi.targetTransform == null) continue;

            float dist = Vector3.Distance(transform.position, poi.targetTransform.position);
            if (dist < distanceToClosest)
            {
                distanceToClosest = dist;
                closestPOI = poi;
            }
        }

        if (closestPOI == null) return;

        if (distanceToClosest >= closestPOI.fullFogDistance)
        {
            if (overlayCamera.enabled) overlayCamera.enabled = false;
        }
        else
        {
            if (!overlayCamera.enabled) overlayCamera.enabled = true;
        }
    }

    private void OnBeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera.name == "BeaconOverlayCAM")
        {
            defaultFogState = RenderSettings.fog;
            defaultFogDensity = RenderSettings.fogDensity;
            RenderSettings.fog = false;
        }
        else if (camera == overlayCamera && closestPOI != null)
        {
            defaultFogState = RenderSettings.fog;
            defaultFogDensity = RenderSettings.fogDensity;

            float distance = Vector3.Distance(transform.position, closestPOI.targetTransform.position);

            if (distance <= closestPOI.clearDistance)
            {
                RenderSettings.fog = false;
            }
            else
            {
                RenderSettings.fog = true;

                // --- THE FIX: Safe Math & Easing Curve ---
                // 1. Clamp ensures the raw percentage NEVER goes below 0.0 or above 1.0
                float rawPercent = Mathf.Clamp01((distance - closestPOI.clearDistance) / (closestPOI.fullFogDistance - closestPOI.clearDistance));

                // 2. SmoothStep curves the transition so it feels natural to the human eye, not rigid and robotic
                float smoothPercent = Mathf.SmoothStep(0f, 1f, rawPercent);

                RenderSettings.fogDensity = Mathf.Lerp(0f, defaultFogDensity, smoothPercent);
            }
        }
    }

    private void OnEndCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera.name == "BeaconOverlayCAM" || camera == overlayCamera)
        {
            RenderSettings.fog = defaultFogState;
            RenderSettings.fogDensity = defaultFogDensity;
        }
    }
}