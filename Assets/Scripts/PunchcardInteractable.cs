using UnityEngine;

/// <summary>
/// A simple interactions script to place on the placeholder Punchcard prefab.
/// Requires a Collider (e.g. BoxCollider) to detect mouse clicks.
/// </summary>
public class PunchcardInteractable : MonoBehaviour
{
    [HideInInspector]
    public CRTWaveController waveController;

    [Header("Dispense Animation")]
    [Tooltip("How far the card should slide out when it spawns.")]
    public float slideDistance = 0.2f;
    [Tooltip("How fast the card slides out (units per second).")]
    public float slideSpeed = 0.5f;
    [Tooltip("The local axis to slide along. (0,0,1) is forward.")]
    public Vector3 slideDirection = Vector3.forward;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isSliding = true;

    void Start()
    {
        // Calculate where we are starting and where we want to end up
        startPosition = transform.position;
        // Move along the local direction vector based on our starting rotation
        Vector3 worldSlideDirection = transform.TransformDirection(slideDirection.normalized);
        targetPosition = startPosition + (worldSlideDirection * slideDistance);
    }

    void Update()
    {
        if (isSliding)
        {
            // Move smoothly towards the target position
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, slideSpeed * Time.deltaTime);

            // Check if we've arrived
            if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
            {
                transform.position = targetPosition;
                isSliding = false;
            }
        }
    }

    void OnMouseOver()
    {
        // Require the player to be looking at (mousing over) the object AND press 'E'
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("<color=cyan>Punchcard Collected via E!</color>");
            
            // Tell the machine to turn off its lights and waves
            if (waveController != null)
            {
                waveController.TurnOffMachine();
            }

            // For right now, collecting it just despawns the object.
            Destroy(gameObject);
        }
    }
}
