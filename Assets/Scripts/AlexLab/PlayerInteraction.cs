using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 3f;
    public LayerMask interactLayer;

    IInteractable currentInteractable;

    void Update()
    {
    // If reading, pressing E closes the note
    if (NoteUIManager.Instance.isReading)
    {
      if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
        {
    NoteUIManager.Instance.CloseNote();
        }   

        return; // stop all other interaction
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

                InteractionPromptUI.Instance.ShowPrompt(interactable.GetPrompt());

                return;
            }
        }

        currentInteractable = null;
        InteractionPromptUI.Instance.HidePrompt();
    }
}