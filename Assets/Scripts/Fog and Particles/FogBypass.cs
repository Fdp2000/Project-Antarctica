using UnityEngine;
using UnityEngine.Rendering;

// 1. This custom class creates the nice layout in your Inspector
[System.Serializable]
public class POISettings
{
    public string locationName = "New POI"; // Just to keep your Inspector organized!
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

    // Internal tracking
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

        // 2. Scan the array to figure out which POI the player is currently closest to
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

        // 3. Turn the overlay camera off if we are completely blinded by the blizzard
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
        // Check if we are rendering the special Beacon camera
        if (camera.name == "BeaconOverlayCAM")
        {
            defaultFogState = RenderSettings.fog;
            defaultFogDensity = RenderSettings.fogDensity;

            // FORCE FOG OFF: This ensures the Red Glow is 100% visible even at 500m
            RenderSettings.fog = false;
        }
        // Otherwise, handle the normal fading interior
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
                float fadePercent = (distance - closestPOI.clearDistance) / (closestPOI.fullFogDistance - closestPOI.clearDistance);
                RenderSettings.fogDensity = Mathf.Lerp(0f, defaultFogDensity, fadePercent);
            }
        }
    }

    private void OnEndCamera(ScriptableRenderContext context, Camera camera)
    {
        // Put the blizzard back for BOTH types of overlay cameras
        if (camera.name == "BeaconOverlayCAM" || camera == overlayCamera)
        {
            RenderSettings.fog = defaultFogState;
            RenderSettings.fogDensity = defaultFogDensity;
        }
    }
}