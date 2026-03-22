using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RoomToneZone : MonoBehaviour
{
    [Header("Room Tone Audio")]
    [Tooltip("The AudioSource playing this room's tone.")]
    public AudioSource roomToneSource;

    [Header("Volume Settings")]
    [Tooltip("The target volume when the player is INSIDE the room.")]
    [Range(0f, 1f)] public float insideVolume = 0.1f;

    [Tooltip("How fast the room tone fades in and out.")]
    public float fadeSpeed = 3.0f;

    private bool isPlayerInside = false;

    private void Start()
    {
        if (roomToneSource != null)
        {
            roomToneSource.volume = 0f; // Start completely silent
            roomToneSource.Play();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }

    private void Update()
    {
        if (roomToneSource == null) return;

        // Target volume is either our 'inside' volume, or absolute zero
        float targetVolume = isPlayerInside ? insideVolume : 0f;

        // Smoothly slide the volume up or down
        roomToneSource.volume = Mathf.Lerp(roomToneSource.volume, targetVolume, fadeSpeed * Time.deltaTime);
    }
}