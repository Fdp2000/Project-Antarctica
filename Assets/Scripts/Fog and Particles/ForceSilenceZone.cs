using UnityEngine;
using UnityEngine.Audio;

public class ForceSilenceZone : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("Drag your DeafeningSilence snapshot here.")]
    public AudioMixerSnapshot silenceSnapshot;

    [Tooltip("If you want the audio to return to normal when you leave, drag the NormalState snapshot here.")]
    public AudioMixerSnapshot normalSnapshot;
    public float fadeTime = 1.5f;

    [Header("Zone Settings")]
    [Tooltip("Check this if this is a permanent change (like an endgame trigger). Uncheck if it's a specific quiet room.")]
    public bool triggerOnlyOnce = true;
    private bool hasTriggered = false;

    // Cache the director so we can turn it back on later if needed
    private MonsterDirector activeDirector;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerOnlyOnce && hasTriggered) return;
            hasTriggered = true;

            Debug.Log("<color=magenta>SILENCE ZONE ENTERED: Forcing Deafening Silence.</color>");

            // 1. Force the audio snapshot
            if (silenceSnapshot != null)
            {
                silenceSnapshot.TransitionTo(fadeTime);
            }

            // 2. The "No Matter What" Override
            // Find the MonsterDirector and shut it off completely so it cannot fight us for audio control!
            activeDirector = FindObjectOfType<MonsterDirector>();
            if (activeDirector != null)
            {
                activeDirector.enabled = false;
                Debug.Log("<color=red>Monster Director automatically disabled to protect audio state.</color>");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Only run the exit logic if the trigger is meant to be repeatable
        if (other.CompareTag("Player") && !triggerOnlyOnce)
        {
            Debug.Log("<color=magenta>EXITING SILENCE ZONE: Restoring Normal Audio.</color>");

            // 1. Restore the normal audio
            if (normalSnapshot != null)
            {
                normalSnapshot.TransitionTo(fadeTime);
            }

            // 2. Turn the MonsterDirector back on so it can resume hunting
            if (activeDirector != null)
            {
                activeDirector.enabled = true;
                Debug.Log("<color=green>Monster Director re-enabled.</color>");
            }
        }
    }
}