using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SnowEventTrigger : MonoBehaviour
{
    public enum EventType
    {
        PlaySound,
        StopSound,
        ToggleSound
    }

    [Header("Event Settings")]
    public EventType eventType;

    [Header("Audio")]
    public AudioSource targetAudio;
    public AudioClip clip;

    [Header("Options")]
    public bool playOnEnter = true;
    public bool playOnExit = false;
    public bool oneShot = true;
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (playOnEnter)
            TriggerEvent();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (playOnExit)
            TriggerEvent();
    }

    void TriggerEvent()
    {
        if (triggerOnce && hasTriggered) return;

        if (targetAudio == null) return;

        switch (eventType)
        {
            case EventType.PlaySound:
                if (oneShot && clip != null)
                {
                    targetAudio.PlayOneShot(clip);
                }
                else
                {
                    if (clip != null) targetAudio.clip = clip;
                    targetAudio.Play();
                }
                break;

            case EventType.StopSound:
                targetAudio.Stop();
                break;

            case EventType.ToggleSound:
                if (targetAudio.isPlaying)
                    targetAudio.Stop();
                else
                    targetAudio.Play();
                break;
        }

        hasTriggered = true;
    }
}