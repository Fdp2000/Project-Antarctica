using UnityEngine;

[CreateAssetMenu(fileName = "NewJournalEntry", menuName = "Game/Journal Entry")]
public class JournalEntry : ScriptableObject
{
    public string entryTitle;

    [TextArea(8,30)]
    public string entryText;
}