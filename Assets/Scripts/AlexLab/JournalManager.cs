using System.Collections.Generic;
using UnityEngine;

public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance;

    public List<JournalEntry> journalEntries = new List<JournalEntry>();

    void Awake()
    {
        Instance = this;
    }

    public void AddEntry(JournalEntry entry)
    {
        // Avoid duplicates if you want
        if (!journalEntries.Contains(entry))
            journalEntries.Add(entry);

        Debug.Log("Journal updated with: " + entry.entryTitle);
    }
}