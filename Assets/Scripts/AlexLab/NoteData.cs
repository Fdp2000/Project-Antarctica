using UnityEngine;

[CreateAssetMenu(fileName = "NewNote", menuName = "Game/Note")]
public class NoteData : ScriptableObject
{
    public string noteTitle;

    [TextArea(10, 30)]
    public string noteText;
    public JournalEntry journalEntry;
}