using UnityEngine;

public class FPSLimiter : MonoBehaviour
{
    void Start()
    {
        // Locks the game to 60 Frames Per Second
        Application.targetFrameRate = 60;
    }
}