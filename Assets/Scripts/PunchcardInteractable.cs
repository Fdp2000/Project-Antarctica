using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Outline))]
public class PunchcardInteractable : MonoBehaviour
{
    [HideInInspector] public CRTWaveController waveController;

    [Header("Dispense Animation")]
    public float slideDistance = 0.2f;
    public float slideSpeed = 0.5f;
    public Vector3 slideDirection = Vector3.forward;

    private Vector3 targetLocalPosition;
    private bool isSliding = true;

    void Start()
    {
        Outline outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        targetLocalPosition = transform.localPosition + (slideDirection.normalized * slideDistance);
    }

    void Update()
    {
        if (isSliding)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetLocalPosition, slideSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.localPosition, targetLocalPosition) < 0.001f)
            {
                transform.localPosition = targetLocalPosition;
                isSliding = false;
            }
        }
    }
}