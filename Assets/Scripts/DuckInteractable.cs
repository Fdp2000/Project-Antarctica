using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class DuckInteractable : MonoBehaviour
{
    [Header("Duck Settings")]
    public AudioClip squeakSound;
    [Tooltip("How many seconds before you can squeeze it again.")]
    public float cooldownTime = 1.0f;
    [Tooltip("How flat it gets when squeezed. 0.5 is half height.")]
    public float squishMultiplier = 0.5f;

    private AudioSource audioSource;
    private Vector3 originalScale;
    private bool isOnCooldown = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        originalScale = transform.localScale;
    }

    public void Interact()
    {
        if (isOnCooldown) return;

        if (squeakSound != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.3f);
            audioSource.PlayOneShot(squeakSound);
        }

        StartCoroutine(SquishRoutine());
    }

    private IEnumerator SquishRoutine()
    {
        isOnCooldown = true;

        // 1. SQUASH & STRETCH (Volume Preservation)
        // If we crush the Z-axis by 50%, we must bulge the X and Y axes outwards!
        float squishZ = originalScale.z * squishMultiplier;

        // Calculate how much "lost" mass we need to push to the sides
        float bulgeFactor = 1f + ((1f - squishMultiplier) * 0.5f);
        float bulgeX = originalScale.x * bulgeFactor;
        float bulgeY = originalScale.y * bulgeFactor;

        Vector3 flattenedScale = new Vector3(bulgeX, bulgeY, squishZ);

        // Instantly smash it
        transform.localScale = flattenedScale;

        float elapsed = 0f;

        // 2. THE ELASTIC RECOVERY (The "Boing" Effect)
        while (elapsed < cooldownTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cooldownTime;

            // Apply a custom mathematical spring curve instead of a robotic linear line
            float elasticT = ElasticEaseOut(t);

            // Use LerpUnclamped so it is allowed to "wobble" past its original size
            transform.localScale = Vector3.LerpUnclamped(flattenedScale, originalScale, elasticT);

            yield return null;
        }

        // Lock it perfectly back to normal
        transform.localScale = originalScale;
        isOnCooldown = false;
    }

    // A standard animation curve formula that simulates a rubber spring settling
    private float ElasticEaseOut(float t)
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        float p = 0.3f; // The "bounciness" period
        return Mathf.Pow(2, -10 * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
    }
}