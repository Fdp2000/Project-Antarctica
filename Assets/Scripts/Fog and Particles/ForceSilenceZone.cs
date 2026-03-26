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

    private MonsterDirector activeDirector;

    private void OnTriggerEnter(Collider other)
    {
        bool isPlayer = other.CompareTag("Player");
        bool isVehicle = other.CompareTag("Vehicle") || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Vehicle"));

        if (isPlayer || isVehicle)
        {
            if (triggerOnlyOnce && hasTriggered) return;
            hasTriggered = true;

            Debug.Log("<color=magenta>SILENCE ZONE ENTERED: Forcing Deafening Silence.</color>");

            if (silenceSnapshot != null)
            {
                silenceSnapshot.TransitionTo(fadeTime);
            }

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
        bool isPlayer = other.CompareTag("Player");
        bool isVehicle = other.CompareTag("Vehicle") || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Vehicle"));

        if ((isPlayer || isVehicle) && !triggerOnlyOnce)
        {
            Debug.Log("<color=magenta>EXITING SILENCE ZONE: Restoring Normal Audio.</color>");

            if (normalSnapshot != null)
            {
                normalSnapshot.TransitionTo(fadeTime);
            }

            if (activeDirector != null)
            {
                activeDirector.enabled = true;
                Debug.Log("<color=green>Monster Director re-enabled.</color>");
            }
        }
    }
}