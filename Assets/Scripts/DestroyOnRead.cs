using UnityEngine;
using System.Collections;

public class DestroyOnRead : MonoBehaviour
{
    [Tooltip("How many seconds to wait after reading before destroying the object. 0 = Instant.")]
    public float destroyDelayTimer = 0.5f;

    // You will call this function from the NoteInteract's "On First Read" Unity Event!
    public void InitiateSelfDestruct()
    {
        if (destroyDelayTimer <= 0f)
        {
            Destroy(gameObject);
        }
        else
        {
            StartCoroutine(DestructRoutine());
        }
    }

    private IEnumerator DestructRoutine()
    {
        yield return new WaitForSeconds(destroyDelayTimer);
        Destroy(gameObject);
    }
}