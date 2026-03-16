using UnityEngine;
using TMPro;

public class NoteUIManager : MonoBehaviour
{
    public static NoteUIManager Instance;

    public GameObject noteCanvas;
    public TMP_Text titleText;
    public TMP_Text bodyText;

    public bool isReading = false;

    void Awake()
    {
        Instance = this;
        noteCanvas.SetActive(false);
    }

    public void ShowNote(NoteData note)
    {
        noteCanvas.SetActive(true);

        titleText.text = note.noteTitle;
        bodyText.text = note.noteText;

        isReading = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseNote()
    {
        noteCanvas.SetActive(false);

        isReading = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}