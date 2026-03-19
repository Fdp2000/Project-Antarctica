using UnityEngine;
using System.Collections;

public class LightFlicker : MonoBehaviour
{
    public Light flickerLight;
    public float normalIntensity = 2f;
    public float flickerIntensity = 0.2f;

    void Start()
    {
        if (flickerLight == null)
            flickerLight = GetComponent<Light>();

        StartCoroutine(FlickerRoutine());
    }

    IEnumerator FlickerRoutine()
    {
        while (true)
        {
            // Normal stable light
            flickerLight.intensity = normalIntensity;
            yield return new WaitForSeconds(Random.Range(1f, 4f));

            // Flicker burst
            int flickers = Random.Range(2, 6);
            for (int i = 0; i < flickers; i++)
            {
                flickerLight.intensity = flickerIntensity;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));

                flickerLight.intensity = normalIntensity;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
            }

            // Occasional full blackout
            if (Random.value > 0.7f)
            {
                flickerLight.enabled = false;
                yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
                flickerLight.enabled = true;
            }
        }
    }
}