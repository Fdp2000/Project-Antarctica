using UnityEngine;

public class WeatherFollower : MonoBehaviour
{
    [Tooltip("Drag your Player or Main Camera here")]
    public Transform player;

    [Tooltip("The global offset. Set Z to be far upwind!")]
    public Vector3 offset = new Vector3(0, 10f, 40f);

    void LateUpdate()
    {
        if (player != null)
        {
            // Follows the player's world position plus the offset, 
            // but maintains a constant 0,0,0 global rotation so the wind never turns!
            transform.position = player.position + offset;
        }
    }
}