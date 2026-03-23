using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Outline))]
public class PunchcardInteractable : MonoBehaviour
{
    [HideInInspector] public CRTWaveController waveController;

    [Header("Dispense Animation")]
    public float slideDistance = 0.0005f;

    [Tooltip("Exactly how many seconds it should take to slide out.")]
    public float slideDuration = 1.5f; // <--- NEW: Use a stopwatch instead of speed!

    public Vector3 slideDirection = new Vector3(-1, 0, 0);

    private Vector3 startLocalPosition;
    private Vector3 targetLocalPosition;
    private float slideTimer = 0f;
    private bool isSliding = true;

    void Start()
    {
        Outline outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        startLocalPosition = transform.localPosition;
        targetLocalPosition = startLocalPosition + (slideDirection.normalized * slideDistance);
    }

    void Update()
    {
        if (isSliding)
        {
            // 1. Run the stopwatch
            slideTimer += Time.deltaTime;

            // 2. Calculate what percentage of the time has passed (0.0 to 1.0)
            float percent = slideTimer / slideDuration;

            // 3. Smoothly slide it based on the percentage
            transform.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, percent);

            // 4. When the stopwatch finishes, snap it exactly and stop!
            if (percent >= 1.0f)
            {
                transform.localPosition = targetLocalPosition;
                isSliding = false;
            }
        }
    }
}