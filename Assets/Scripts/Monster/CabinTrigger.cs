using UnityEngine;

public class CabinTrigger : MonoBehaviour
{
    public MonsterDirector monsterDirector;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && monsterDirector != null)
        {
            monsterDirector.isPlayerInCabin = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && monsterDirector != null)
        {
            monsterDirector.isPlayerInCabin = false;
        }
    }
}