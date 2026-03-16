using UnityEngine;

public class NoteInteract : MonoBehaviour, IInteractable
{
    public NoteData noteData;

    public string promptText = "[E] Read Note";

    public void Interact()
    {
        NoteViewer.Instance.ShowNote(noteData);
    }

    public string GetPrompt()
    {
        return promptText;
    }
}