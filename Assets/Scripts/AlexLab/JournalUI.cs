using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class JournalUI : MonoBehaviour
{
    public GameObject journalPanel;

    public Transform entryListParent;
    public GameObject entryButtonPrefab;

    public TMP_Text entryTitleText;
    public TMP_Text entryBodyText;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            ToggleJournal();
        }
    }

    void ToggleJournal()
    {
        bool open = !journalPanel.activeSelf;

        journalPanel.SetActive(open);

        if (open)
            PopulateEntryList();

        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    void PopulateEntryList()
    {
        foreach (Transform child in entryListParent)
            Destroy(child.gameObject);

        foreach (JournalEntry entry in JournalManager.Instance.journalEntries)
        {
            GameObject buttonObj = Instantiate(entryButtonPrefab, entryListParent);

            TMP_Text text = buttonObj.GetComponentInChildren<TMP_Text>();
            text.text = entry.entryTitle;

            Button btn = buttonObj.GetComponent<Button>();

            btn.onClick.AddListener(() =>
            {
                ShowEntry(entry);
            });
        }
    }

    void ShowEntry(JournalEntry entry)
    {
        entryTitleText.text = entry.entryTitle;
        entryBodyText.text = entry.entryText;
    }
}