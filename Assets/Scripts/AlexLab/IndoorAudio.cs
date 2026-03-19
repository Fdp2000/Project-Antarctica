using UnityEngine;

public class IndoorAudio : MonoBehaviour
{
    public FootstepAudio playerFootsteps; // Reference to the FootstepAudio component


    public AudioLowPassFilter[] filters;
    public float indoorCutoff = 3000f;
    public float outdoorCutoff = 22000f;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Something entered: " + other.name);
        if (other.CompareTag("Player"))
        {   
            Debug.Log("Entered building trigger");
            SetIndoor(true);
            playerFootsteps.SetIndoor(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Exited building trigger");
            SetIndoor(false);
            playerFootsteps.SetIndoor(false);
        }
    }

    void SetIndoor(bool indoors)
    {
        foreach (var f in filters)
        {
            f.cutoffFrequency = indoors ? indoorCutoff : outdoorCutoff;
        }
    }
}