using UnityEngine;
using UnityEngine.Events; // <--- 1. We need this to use Unity Events!

public class NoteInteract : MonoBehaviour, IInteractable
{
    public NoteData noteData;
    public string promptText = "[E] Read Note";

    [Header("Custom Events")]
    [Tooltip("What happens the very first time the player reads this?")]
    public UnityEvent onFirstRead; // <--- 2. The magic event variable
    private bool hasBeenRead = false; // <--- 3. Tracker so it only happens once

    public void Interact()
    {
        // --- NEW: Trigger the custom event on the very first read ---
        if (!hasBeenRead)
        {
            onFirstRead?.Invoke();
            hasBeenRead = true;
        }

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