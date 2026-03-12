using UnityEngine;

/// <summary>
/// A simple component to attach to a 3D object in the world.
/// Defines the specific transmission characteristics of a given Point of Interest.
/// </summary>
public class RadioBeacon : MonoBehaviour
{
    [Tooltip("The frequency (in MHz) this beacon broadcasts on.")]
    public float broadcastFrequency = 88.5f;

    [Tooltip("The specific sound this beacon transmits, like Morse code or telemetry data.")]
    public AudioClip broadcastPayload;
}
