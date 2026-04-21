using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TeleportLocation
{
    public string locationName = "New POI";
    [Tooltip("The first key you must hold down (e.g., LeftControl)")]
    public KeyCode modifierKey = KeyCode.LeftControl;
    [Tooltip("The second key to trigger the teleport (e.g., Alpha1)")]
    public KeyCode actionKey = KeyCode.Alpha1;

    [Header("Player Coordinates")]
    public Vector3 playerPosition;
    [Tooltip("Rotation in standard degrees (X, Y, Z)")]
    public Vector3 playerRotation;

    [Header("Cruiser Coordinates")]
    public Vector3 cruiserPosition;
    [Tooltip("Rotation in standard degrees (X, Y, Z)")]
    public Vector3 cruiserRotation;
}

public class DebugTeleporter : MonoBehaviour
{
    [Header("Entities to Teleport")]
    public Transform player;
    public Transform cruiser;

    [Header("UI & Visuals")]
    public Image blackScreen;

    [Header("Fade Settings")]
    public float fadeOutDuration = 0.5f;
    public float timeInBlack = 0.2f;
    public float fadeInDuration = 0.5f;

    [Header("Locations Library")]
    public List<TeleportLocation> locations = new List<TeleportLocation>();

    private bool isTeleporting = false;
    private CharacterController playerCC;
    private Rigidbody cruiserRb;

    void Start()
    {
        if (player != null) playerCC = player.GetComponent<CharacterController>();
        if (cruiser != null) cruiserRb = cruiser.GetComponent<Rigidbody>();

        // Ensure the black screen is completely transparent and not blocking clicks on start
        if (blackScreen != null)
        {
            Color startColor = blackScreen.color;
            startColor.a = 0f;
            blackScreen.color = startColor;
            blackScreen.raycastTarget = false;
        }
    }

    void Update()
    {
        // Don't listen for inputs if we are already in the middle of a teleport
        if (isTeleporting) return;

        // Check every location in our list for its specific key combo
        foreach (var loc in locations)
        {
            if (Input.GetKey(loc.modifierKey) && Input.GetKeyDown(loc.actionKey))
            {
                StartCoroutine(TeleportSequence(loc));
                break; // Stop checking once we find a match
            }
        }
    }

    private IEnumerator TeleportSequence(TeleportLocation targetLoc)
    {
        isTeleporting = true;
        Debug.Log($"<color=cyan>DEBUG: Teleporting to {targetLoc.locationName}...</color>");

        // 1. FADE OUT
        if (blackScreen != null)
        {
            blackScreen.raycastTarget = true; // Block UI clicks during fade
            float elapsed = 0f;
            Color c = blackScreen.color;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration);
                blackScreen.color = c;
                yield return null;
            }
            c.a = 1f;
            blackScreen.color = c;
        }

        // 2. EXECUTE TELEPORT PHYSICS

        // Player: Must disable CharacterController before moving, or it snaps back!
        if (playerCC != null) playerCC.enabled = false;
        if (player != null)
        {
            player.position = targetLoc.playerPosition;
            player.rotation = Quaternion.Euler(targetLoc.playerRotation);
        }
        if (playerCC != null) playerCC.enabled = true;

        // Cruiser: Move position and kill all momentum so it doesn't fly away
        if (cruiser != null)
        {
            cruiser.position = targetLoc.cruiserPosition;
            cruiser.rotation = Quaternion.Euler(targetLoc.cruiserRotation);

            if (cruiserRb != null)
            {
                cruiserRb.position = targetLoc.cruiserPosition;
                cruiserRb.rotation = Quaternion.Euler(targetLoc.cruiserRotation);
                cruiserRb.linearVelocity = Vector3.zero;
                cruiserRb.angularVelocity = Vector3.zero;
            }
        }

        // Force Unity to update the invisible hitboxes immediately
        Physics.SyncTransforms();

        // 3. WAIT IN DARKNESS
        yield return new WaitForSeconds(timeInBlack);

        // 4. FADE IN
        if (blackScreen != null)
        {
            float elapsed = 0f;
            Color c = blackScreen.color;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
                blackScreen.color = c;
                yield return null;
            }
            c.a = 0f;
            blackScreen.color = c;
            blackScreen.raycastTarget = false; // Allow UI clicks again
        }

        isTeleporting = false;
    }
}