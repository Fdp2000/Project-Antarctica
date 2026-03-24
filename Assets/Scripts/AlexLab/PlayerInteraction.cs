using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 3f;
    public LayerMask interactLayer;

    IInteractable currentInteractable;

    void Update()
{
    // If reading, pressing E closes the note
    if (NoteViewer.Instance != null && NoteViewer.Instance.isReading)
    {
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
        {
            NoteViewer.Instance.CloseNote();
        }

        return; // stop all other interaction/movement
    }

    CheckForInteractable();

    if (currentInteractable != null && Input.GetKeyDown(KeyCode.E))
    {
        currentInteractable.Interact();
    }
}

    void CheckForInteractable()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                currentInteractable = interactable;


                return;
            }
        }

        currentInteractable = null;
    }
}