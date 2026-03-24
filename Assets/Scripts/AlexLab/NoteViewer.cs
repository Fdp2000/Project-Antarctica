using UnityEngine;
using TMPro;

public class NoteViewer : MonoBehaviour
{
    public static NoteViewer Instance;

    [Header("Settings")]
    public GameObject notePrefab;      // Your World Space Canvas prefab
    public float spawnDistance = 0.4f; // Distance in front of camera
    public float spawnHeight = 0.1f;   // Slightly above camera center
    public float rotationSpeed = 50f;  // Mouse rotation while reading
    public float smoothFaceSpeed = 10f; // Smooth rotation speed

    [Header("UI")]
    public GameObject interactionPrompt; // Drag your [E] Read canvas here
    public GameObject crosshair; // Optional: Hide crosshair while reading

    [Header("Sounds")]
    public AudioClip openNoteSFX;
    public AudioClip closeNoteSFX;
    private AudioSource audioSource;


    private GameObject currentNote;
    public bool isReading = false;

    private Camera mainCamera;

    void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
    if (NoteViewer.Instance != null && NoteViewer.Instance.isReading)
    {
    // Skip all movement/rotation while reading
    return;
    }

    // Rotate note slightly with mouse
    if (currentNote != null)
    {
        float rotX = -Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime*0.2f;
        float rotY = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime*0.2f;

        currentNote.transform.Rotate(rotX, rotY, 0, Space.Self);
    }
    }

    public void ShowNote(NoteData note)
    {
    if (currentNote != null) return;

    if (audioSource != null && openNoteSFX != null)
        audioSource.PlayOneShot(openNoteSFX);

    // Hide the prompt
    if (interactionPrompt != null)
        interactionPrompt.SetActive(false);

    Vector3 spawnPos = mainCamera.transform.position +
                       mainCamera.transform.forward * spawnDistance +
                       mainCamera.transform.up * spawnHeight;

    currentNote = Instantiate(notePrefab, spawnPos, Quaternion.identity);

    // Make it face camera once
    currentNote.transform.LookAt(mainCamera.transform);
    currentNote.transform.Rotate(0, 180f, 0);

    currentNote.transform.SetParent(mainCamera.transform);

    TMP_Text text = currentNote.GetComponentInChildren<TMP_Text>();
    if (text != null)
        text.text = note.noteText;

    if (note.journalEntry != null)
        JournalManager.Instance.AddEntry(note.journalEntry);

    isReading = true;
    Cursor.lockState = CursorLockMode.None;
        crosshair.SetActive(false);

    }

    public void CloseNote()
    {
    if (currentNote != null)
        Destroy(currentNote);

     // Play close sound
    if (audioSource != null && closeNoteSFX != null)
        audioSource.PlayOneShot(closeNoteSFX);

    // Show the prompt again
    if (interactionPrompt != null)
        interactionPrompt.SetActive(true);

    isReading = false;
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
        if (crosshair != null)
        crosshair.SetActive(true);
    }
}