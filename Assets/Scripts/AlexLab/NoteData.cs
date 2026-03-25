using UnityEngine;

[CreateAssetMenu(fileName = "NewNote", menuName = "Game/Note")]
public class NoteData : ScriptableObject
{
    public string noteTitle;

    [Header("Text Settings")]
    [Tooltip("The size of the font when reading this specific note.")]
    public float fontSize = 5.5f; // <--- NEW

    [TextArea(10, 30)]
    public string noteText;

    public JournalEntry journalEntry;
}