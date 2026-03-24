using UnityEngine;

public class NoteInteract : MonoBehaviour, IInteractable
{
    public NoteData noteData;
    public string promptText = "[E] Read Note";

    public void Interact()
    {
        // 1. Grab the material from this 3D note's MeshRenderer
        Material worldMaterial = null;
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            worldMaterial = meshRenderer.material;
        }

        // 2. Pass the material to the NoteViewer along with the data
        NoteViewer.Instance.ShowNote(noteData, worldMaterial);
    }

    public string GetPrompt()
    {
        return promptText;
    }
}