using UnityEngine;

public class OutlineRaycast : MonoBehaviour
{
    public float rayDistance = 5f;

    private Outline currentOutline;

    void Update()
{
    // 🚫 Stop outlines while reading
    if (NoteViewer.Instance != null && NoteViewer.Instance.isReading)
    {
        ClearOutline();
        return;
    }

    Ray ray = new Ray(transform.position, transform.forward);
    RaycastHit hit;

    if (Physics.Raycast(ray, out hit, rayDistance))
    {
        Outline outline = hit.collider.GetComponentInParent<Outline>();

        if (outline != null)
        {
            if (currentOutline != outline)
            {
                ClearOutline();

                currentOutline = outline;
                currentOutline.OutlineWidth = 5f;
            }
            return;
        }
    }

    ClearOutline();
}

    void ClearOutline()
    {
        if (currentOutline != null)
        {
            currentOutline.OutlineWidth = 0f; // HIDE instead of disable
            currentOutline = null;
        }
    }
}