using UnityEngine;

public class ClutchMonsterVisuals : MonoBehaviour
{
    [Header("Dependencies")]
    public WinchController winchController;
    public GameObject visualRoot; // The Monster Model

    [Header("Clipping Prevention")]
    [Tooltip("The door angle where the monster instantly hides so it doesn't clip into the frame.")]
    public float hideAngle = -110f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip spawnRoarSound;
    [Range(0f, 1f)] public float spawnVolume = 1.0f;

    private bool isStruggleActive = false;
    private bool isHiddenDueToClipping = false;

    void Start()
    {
        if (visualRoot != null) visualRoot.SetActive(false);
    }

    void Update()
    {
        if (!isStruggleActive || visualRoot == null || winchController == null) return;

        // Simple clipping prevention: hide if the door closes past the hideAngle
        if (winchController.CurrentAngle > hideAngle)
        {
            if (!isHiddenDueToClipping)
            {
                visualRoot.SetActive(false);
                isHiddenDueToClipping = true;
            }
        }
        else
        {
            if (isHiddenDueToClipping)
            {
                visualRoot.SetActive(true);
                isHiddenDueToClipping = false;
            }
        }
    }

    public void ShowMonster()
    {
        if (visualRoot != null)
        {
            visualRoot.SetActive(true);

            // --- THE REVERSE SCALE FIX ---
            // Counteract the parent's massive scale so the monster looks perfectly normal!
            if (transform.parent != null)
            {
                Vector3 pScale = transform.parent.localScale;
                transform.localScale = new Vector3(1f / pScale.x, 1f / pScale.y, 1f / pScale.z);
            }
        }

        isHiddenDueToClipping = false;

        if (audioSource != null && spawnRoarSound != null)
        {
            audioSource.PlayOneShot(spawnRoarSound, spawnVolume);
        }

        isStruggleActive = true;
    }

    public void HideMonsterInstantly()
    {
        if (visualRoot != null) visualRoot.SetActive(false);
        isStruggleActive = false;
    }
}