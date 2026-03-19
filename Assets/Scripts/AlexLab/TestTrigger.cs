using UnityEngine;
public class TestTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger hit: " + other.name);
    }
}