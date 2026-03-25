using UnityEngine;
using System.Collections;

public class FinalMachineStarter : MonoBehaviour, IInteractable
{
    [Header("Dependencies")]
    public FinalCRTWaveController finalCRT;

    [Header("Punchcard Animation")]
    public GameObject punchcardPrefab;
    [Tooltip("Where the card spawns (just outside the slot)")]
    public Transform slideStartNode;
    [Tooltip("Where the card ends up (deep inside the slot)")]
    public Transform slideEndNode;
    public float slideDuration = 0.8f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip insertSound;

    private bool hasBeenActivated = false;

    public void Interact()
    {
        if (hasBeenActivated) return;
        StartCoroutine(ActivationRoutine());
    }

    public string GetPrompt()
    {
        return hasBeenActivated ? "" : "[E] Insert Final Punchcard";
    }

    private IEnumerator ActivationRoutine()
    {
        hasBeenActivated = true;

        // --- NEW: Permanently remove the Outline so it can never be highlighted again ---
        Outline outlineComponent = GetComponent<Outline>();
        if (outlineComponent != null)
        {
            Destroy(outlineComponent);
        }

        GameObject card = null;

        // 1. Spawn the card at the starting node
        if (punchcardPrefab != null && slideStartNode != null)
        {
            card = Instantiate(punchcardPrefab, slideStartNode.position, slideStartNode.rotation);
            card.transform.localScale = punchcardPrefab.transform.localScale;
        }

        // 2. Play the insertion sound
        if (audioSource != null && insertSound != null)
        {
            audioSource.PlayOneShot(insertSound);
        }

        // 3. Smoothly animate it sliding into the machine
        if (card != null && slideStartNode != null && slideEndNode != null)
        {
            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / slideDuration;

                card.transform.position = Vector3.Lerp(slideStartNode.position, slideEndNode.position, percent);
                yield return null;
            }
            card.transform.position = slideEndNode.position;
        }

        // 4. Dramatic pause before the machine whirs to life
        yield return new WaitForSeconds(0.4f);

        // 5. Trigger the final isolated minigame!
        if (finalCRT != null)
        {
            finalCRT.StartFinalMinigame();
        }
    }
}