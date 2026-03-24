using UnityEngine;

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
        // Cache the layer IDs so we don't have to look them up every time
        defaultLayer = LayerMask.NameToLayer("Default");
        interiorLayer = LayerMask.NameToLayer(interiorLayerName);

        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Player walked inside! Swap the whole building to the Overlay Camera layer
            SetLayerRecursively(interiorEnvironment, interiorLayer);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Player walked outside! Swap the building back to the Default layer (Blizzard)
            SetLayerRecursively(interiorEnvironment, defaultLayer);
        }
    }

    // This loops through the parent object and changes the layer of every single child prop/wall
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}