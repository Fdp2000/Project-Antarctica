using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Collider))]
public class InteriorCameraFog : MonoBehaviour
{
    [Header("Camera Setup")]
    public Camera interiorCamera;

    [Header("Fog Transition Settings")]
    public float fadeDuration = 2.0f;
    public string playerTag = "Player";

    private float globalFogDensity;
    private float currentInteriorDensity;
    private Coroutine fadeCoroutine;

    void Start()
    {
        globalFogDensity = RenderSettings.fogDensity;
        currentInteriorDensity = globalFogDensity;
    }

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        RenderPipelineManager.endCameraRendering += OnEndCamera;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        RenderPipelineManager.endCameraRendering -= OnEndCamera;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            globalFogDensity = RenderSettings.fogDensity;

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeInteriorFog(currentInteriorDensity, 0f));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            globalFogDensity = RenderSettings.fogDensity;

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeInteriorFog(currentInteriorDensity, globalFogDensity));
        }
    }

    private System.Collections.IEnumerator FadeInteriorFog(float startDensity, float targetDensity)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            currentInteriorDensity = Mathf.Lerp(startDensity, targetDensity, elapsed / fadeDuration);
            yield return null;
        }
        currentInteriorDensity = targetDensity;
    }

    // ==========================================
    // --- THE UNITY 6 GPU HIJACK ---
    // ==========================================

    private void OnBeginCamera(ScriptableRenderContext context, Camera cam)
    {
        if (cam == interiorCamera)
        {
            if (currentInteriorDensity <= 0.0001f)
            {
                // Force the GPU to completely disable the fog shaders for this camera
                Shader.DisableKeyword("FOG_EXP");
                Shader.DisableKeyword("FOG_EXP2");
                Shader.DisableKeyword("FOG_LINEAR");
            }
            else
            {
                // Force the GPU to use our custom density math instead of the cached Render Graph math
                float d = currentInteriorDensity;
                Shader.SetGlobalVector("_FogParams", new Vector4(d / 1.1774f, d / 0.6931f, 0, 0));
            }
        }
    }

    private void OnEndCamera(ScriptableRenderContext context, Camera cam)
    {
        if (cam == interiorCamera)
        {
            // The millisecond the interior finishes drawing, we MUST put the GPU back 
            // to the normal global state so the blizzard outside doesn't break!
            if (RenderSettings.fog)
            {
                if (RenderSettings.fogMode == FogMode.ExponentialSquared) Shader.EnableKeyword("FOG_EXP2");
                else if (RenderSettings.fogMode == FogMode.Exponential) Shader.EnableKeyword("FOG_EXP");
                else Shader.EnableKeyword("FOG_LINEAR");

                float d = RenderSettings.fogDensity;
                Shader.SetGlobalVector("_FogParams", new Vector4(d / 1.1774f, d / 0.6931f, 0, 0));
            }
        }
    }
}