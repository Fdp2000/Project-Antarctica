using UnityEngine;
using System.Collections;

public class SparkLightSync : MonoBehaviour
{
    [Header("References")]
    public ParticleSystem sparks;
    public Light flickerLight;
    public AudioSource audioSource;

    [Header("Timing")]
    public float minDelay = 1f;
    public float maxDelay = 4f;

    [Header("Light")]
    public float normalIntensity = 1.5f;
    public float flashIntensity = 5f;
    public float flashDuration = 0.05f;

    [Header("Sound")]
    public AudioClip[] sparkSounds;

    void Start()
    {
        flickerLight.intensity = normalIntensity;
        StartCoroutine(SyncRoutine());
    }

    IEnumerator SyncRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
            StartCoroutine(SparkEvent());
        }
    }

    IEnumerator SparkEvent()
    {
        int bursts = Random.Range(2, 5);

        for (int i = 0; i < bursts; i++)
        {
            // 🔥 Sparks
            sparks.Emit(Random.Range(4, 8));

            // 💡 Light
            flickerLight.intensity = flashIntensity;

            // 🔊 Sound (random clip)
            if (sparkSounds.Length > 0)
            {
                AudioClip clip = sparkSounds[Random.Range(0, sparkSounds.Length)];
                audioSource.PlayOneShot(clip, Random.Range(0.8f, 1.2f));
            }

            yield return new WaitForSeconds(flashDuration);

            flickerLight.intensity = normalIntensity;

            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }
    }
}