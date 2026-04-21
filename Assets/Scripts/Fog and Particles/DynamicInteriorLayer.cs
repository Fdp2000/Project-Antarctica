using UnityEngine;
// --- REQUIRED: Gives us access to the DecalProjector component ---
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Collider))]
public class DynamicInteriorLayer : MonoBehaviour
{
    [Header("The Environment")]
    [Tooltip("Drag the parent object that holds all the interior walls, floors, and props here.")]
    public GameObject interiorEnvironment;

    [Header("Layer Settings")]
    [Tooltip("The exact name of the layer your Overlay Camera renders (e.g., CruiserInterior)")]
    public string interiorLayerName = "CruiserInterior";

    private int defaultLayer;
    private int interiorLayer;

    void Start()
    {
        defaultLayer = LayerMask.NameToLayer("Default");
        interiorLayer = LayerMask.NameToLayer(interiorLayerName);
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SetLayerRecursively(interiorEnvironment, interiorLayer);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SetLayerRecursively(interiorEnvironment, defaultLayer);
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        // ==========================================
        // --- THE UNITY BUG FIX ---
        // Grab the Decal component if it exists, and turn it off!
        // ==========================================
        DecalProjector decal = obj.GetComponent<DecalProjector>();
        if (decal != null) decal.enabled = false;

        // Change the layer
        obj.layer = newLayer;

        // Turn the Decal back on so it re-registers with the new Overlay Camera!
        if (decal != null) decal.enabled = true;

        // Continue down the folder tree
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}