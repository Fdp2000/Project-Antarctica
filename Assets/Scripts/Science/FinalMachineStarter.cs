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

    [Tooltip("How long it takes for the card to slide into the machine.")]
    public float slideDuration = 0.8f;

    [Tooltip("How long to wait AFTER the card stops before the screen turns on.")]
    public float delayBeforeBoot = 0.5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip insertSound;
    [Tooltip("How long to wait before playing the insertion sound.")]
    public float audioDelay = 0.15f; // <--- NEW

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

        // 2. Play the insertion sound using our new delayed timer
        if (audioSource != null && insertSound != null)
        {
            StartCoroutine(PlayDelayedAudio());
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

            // Lock it perfectly into the final position
            card.transform.position = slideEndNode.position;

            // --- REVERTED: The card is no longer destroyed! It stays in the machine. ---
        }

        // 4. Dramatic, customizable pause before the machine whirs to life
        if (delayBeforeBoot > 0f)
        {
            yield return new WaitForSeconds(delayBeforeBoot);
        }

        // 5. Trigger the final isolated minigame!
        if (finalCRT != null)
        {
            finalCRT.StartFinalMinigame();
        }
    }

    // --- NEW: A tiny parallel routine to handle the audio delay seamlessly ---
    private IEnumerator PlayDelayedAudio()
    {
        if (audioDelay > 0f)
        {
            yield return new WaitForSeconds(audioDelay);
        }
        audioSource.PlayOneShot(insertSound);
    }
}