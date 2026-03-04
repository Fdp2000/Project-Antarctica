using UnityEngine;

public class WinchController : MonoBehaviour
{
    [Header("Door Settings")]
    public Transform doorHinge;
    public float openAngle = 80f;
    public float closedAngle = 0f;
    public float closeSpeed = 25f;

    private float currentAngle;

    void Start()
    {
        if (doorHinge == null)
        {
            Debug.LogError("CRITICAL: The WinchController doesn't know what door to move! Drag the Door_Hinge into the Inspector slot.");
        }

        currentAngle = openAngle;
        ApplyRotation();
    }

    public void HoldToClose()
    {
        if (doorHinge == null) return;

        // Mathf.MoveTowards automatically figures out if it needs to go UP or DOWN!
        currentAngle = Mathf.MoveTowards(currentAngle, closedAngle, closeSpeed * Time.deltaTime);
        ApplyRotation();

        Debug.Log("Winching... Current Angle: " + currentAngle.ToString("F1"));

        if (currentAngle == closedAngle)
        {
            Debug.Log("Door is fully closed!");
        }
    }

    public void ClickToOpen()
    {
        if (doorHinge == null) return;

        // Check if we are close to the closed angle (Mathf.Abs makes sure it works for positive and negative!)
        if (Mathf.Abs(currentAngle - closedAngle) <= 5f)
        {
            currentAngle = openAngle;
            ApplyRotation();
            Debug.Log("DOOR SLAMMED OPEN!");
        }
        else
        {
            Debug.Log("Cannot open: Door is not fully closed yet. (Current angle: " + currentAngle + ")");
        }
    }

    private void ApplyRotation()
    {
        if (doorHinge != null)
        {
            doorHinge.localRotation = Quaternion.Euler(currentAngle, 0, 0);
        }
    }
}