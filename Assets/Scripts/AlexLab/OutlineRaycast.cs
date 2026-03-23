using UnityEngine;

public class OutlineRaycast : MonoBehaviour
{
    public float rayDistance = 5f;
    public float outlineTargetWidth = 5f;
    public float smoothSpeed = 10f;
    public LayerMask interactLayer;   // FIX 3
    public float raycastInterval = 0.05f; // FIX 2

    private float raycastTimer;


    private Outline currentOutline;

    void Start()
    {
        // Disable all outlines at start
        foreach (var o in FindObjectsOfType<Outline>())
            o.OutlineWidth = 0f;
    }

    void Update()
{
    if (NoteViewer.Instance != null && NoteViewer.Instance.isReading)
    {
        ClearOutline();
        return;
    }

    // ⏱️ FIX 2: Limit how often raycast runs
    raycastTimer += Time.deltaTime;
    if (raycastTimer < raycastInterval)
        return;

    raycastTimer = 0f;

    Ray ray = new Ray(transform.position, transform.forward);
    RaycastHit hit;

    Outline targetOutline = null;

    // 🎯 FIX 3: Only hit interactable layer
    if (Physics.Raycast(ray, out hit, rayDistance, interactLayer))
    {
        targetOutline = hit.collider.GetComponentInParent<Outline>();
    }

    // Switch target if needed
    if (currentOutline != targetOutline)
    {
        ClearOutline();
        currentOutline = targetOutline;
    }

    // Smoothly grow outline
    if (currentOutline != null)
    {
        currentOutline.OutlineWidth = Mathf.Lerp(
            currentOutline.OutlineWidth,
            outlineTargetWidth,
            Time.deltaTime * smoothSpeed
        );
    }
}

    void ClearOutline()
    {
        if (currentOutline != null)
        {
            // Smoothly shrink instead of snapping
            currentOutline.OutlineWidth = Mathf.Lerp(
                currentOutline.OutlineWidth,
                0f,
                Time.deltaTime * smoothSpeed
            );

            // Fully clear when almost invisible
            if (currentOutline.OutlineWidth < 0.05f)
            {
                currentOutline.OutlineWidth = 0f;
                currentOutline = null;
            }
        }
    }
}