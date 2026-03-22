using UnityEngine;

public class CabinTrigger : MonoBehaviour
{
    [Header("Dependencies")]
    public MonsterDirector monsterDirector;
    public WinchController winchController;
    public SimpleFPSController playerController; // <--- NEW: Added this link

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Inform the Monster Director (for Strike Logic)
            if (monsterDirector != null)
            {
                monsterDirector.isPlayerInCabin = true;
            }

            // Inform the Winch Controller (to pause the wind slam timer)
            if (winchController != null)
            {
                winchController.isPlayerInside = true;
            }

            // --- NEW: Inform the Player Controller (for Engine Camera Shake) ---
            if (playerController != null)
            {
                playerController.isInCabin = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Inform the Monster Director
            if (monsterDirector != null)
            {
                monsterDirector.isPlayerInCabin = false;
            }

            // Inform the Winch Controller (starts the wind slam timer if door is shut)
            if (winchController != null)
            {
                winchController.isPlayerInside = false;
            }

            // --- NEW: Inform the Player Controller ---
            if (playerController != null)
            {
                playerController.isInCabin = false;
            }
        }
    }
}