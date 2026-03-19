using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteriorZone : MonoBehaviour
{
    [Header("Fog Settings")]
    [Tooltip("How thick the fog is inside the building (usually 0).")]
    public float indoorFogDensity = 0.0f;
    [Tooltip("How fast the weather fades in and out when crossing the door.")]
    public float transitionSpeed = 3.0f;

    private float outdoorFogDensity;
    private bool playerIsInside = false;

    void Start()
    {
        // Remember how brutal the blizzard is so we can put it back when you leave
        outdoorFogDensity = RenderSettings.fogDensity;

        // Ensure the collider is set to Trigger so the player doesn't bounce off it
        GetComponent<Collider>().isTrigger = true;
    }

    void Update()
    {
        // Smoothly transition the Global Fog
        float targetDensity = playerIsInside ? indoorFogDensity : outdoorFogDensity;
        RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetDensity, Time.deltaTime * transitionSpeed);

        // If the fog is basically 0, turn it completely off to save GPU power
        if (RenderSettings.fogDensity <= 0.001f)
        {
            RenderSettings.fog = false;
        }
        else
        {
            RenderSettings.fog = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // IMPORTANT: Make sure your player object has the "Player" Tag!
        if (other.CompareTag("Player"))
        {
            playerIsInside = true;
            // TODO: Muffle wind audio here later!
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInside = false;
            // TODO: Restore wind audio here later!
        }
    }
}