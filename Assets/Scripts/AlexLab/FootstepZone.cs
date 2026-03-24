using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FootstepZone : MonoBehaviour
{
    public FootstepSystem.FootstepMode mode = FootstepSystem.FootstepMode.Researchbase;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FootstepSystem fs = other.GetComponent<FootstepSystem>();
            if (fs != null)
            {
                fs.SetFootstepMode(mode);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FootstepSystem fs = other.GetComponent<FootstepSystem>();
            if (fs != null)
            {
                fs.SetFootstepMode(FootstepSystem.FootstepMode.Outdoor);
            }
        }
    }
}