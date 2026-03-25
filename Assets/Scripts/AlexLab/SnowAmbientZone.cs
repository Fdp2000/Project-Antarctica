using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class SnowAmbientZone : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] clips;

    [Header("Timing")]
    public float minDelay = 3f;
    public float maxDelay = 10f;

    [Header("Settings")]
    public bool randomPitch = true;
    public Vector2 pitchRange = new Vector2(0.9f, 1.1f);

    private bool playerInside = false;
    private Coroutine loopCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Player entered Snow Ambient Zone.");
        if (!other.CompareTag("Player")) return;

        playerInside = true;

        if (loopCoroutine == null)
            loopCoroutine = StartCoroutine(PlayLoop());
    }



    IEnumerator PlayLoop()
    {
        while (playerInside)
        {
            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);

            if (!playerInside) yield break;

            PlayRandomSound();
            Debug.Log("Played snow ambient sound.");
        }
    }

    void PlayRandomSound()
    {
        if (clips == null || clips.Length == 0 || audioSource == null) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];

        if (randomPitch)
            audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        else
            audioSource.pitch = 1f;

        audioSource.PlayOneShot(clip);
    }
}