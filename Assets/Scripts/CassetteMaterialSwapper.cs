using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class CassetteMaterialSwapper : MonoBehaviour
{
    private MeshRenderer meshRenderer;

    [Tooltip("Which material slot on the mesh is the sticker/label? (Usually 0)")]
    public int targetMaterialIndex = 0;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SetTapeMaterial(Material newMaterial)
    {
        if (meshRenderer == null || newMaterial == null) return;

        // In Unity, you have to extract the whole array, modify it, and assign it back
        Material[] mats = meshRenderer.materials;
        if (mats.Length > targetMaterialIndex)
        {
            mats[targetMaterialIndex] = newMaterial;
            meshRenderer.materials = mats;
        }
        else
        {
            Debug.LogWarning("Material Swapper failed: The Target Material Index is out of bounds for this mesh!");
        }
    }
}