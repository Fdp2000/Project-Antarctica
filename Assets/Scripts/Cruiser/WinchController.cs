using System;
using UnityEngine;

[RequireComponent(typeof(Outline))]
public class WinchController : MonoBehaviour
{
    public event Action OnDoorFullyOpened;
    public event Action OnDoorFullyClosed;
    [Header("Door Settings")]
    public Transform doorHinge;
    public float openAngle = 80f;
    public float closedAngle = 0f;
    public float closeSpeed = 25f;

    [Header("Interaction Timers")]
    public float openHoldTime = 0.25f;
    public float closeCooldownTime = 1.2f;
    public float openCooldownTime = 2.0f;

    private float currentAngle;
    private float currentOpenHold = 0f;
    private float currentCloseCooldown = 0f;
    private float currentOpenCooldown = 0f;
    private bool isBeingHeldThisFrame = false;

    // Public getters to allow other scripts to securely ask about the door state
    public bool IsDoorOpen => Mathf.Abs(currentAngle - openAngle) < 0.1f;
    public bool IsDoorClosed => Mathf.Abs(currentAngle - closedAngle) < 0.1f;

    void Start()
    {
        Outline outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        if (doorHinge == null)
        {
            Debug.LogError("CRITICAL: The WinchController doesn't know what door to move! Drag the Door_Hinge into the Inspector slot.");
        }

        currentAngle = openAngle;
        ApplyRotation();
    }

    void Update()
    {
        if (currentCloseCooldown > 0f)
        {
            currentCloseCooldown -= Time.deltaTime;
        }

        if (currentOpenCooldown > 0f)
        {
            currentOpenCooldown -= Time.deltaTime;
        }

        if (!isBeingHeldThisFrame)
        {
            currentOpenHold = 0f; // Reset if they let go of 'E' early
        }

        isBeingHeldThisFrame = false; // Reset for next frame
    }

    public void InteractWinch()
    {
        if (doorHinge == null) return;
        
        isBeingHeldThisFrame = true;

        if (IsDoorClosed)
        {
            // Door is currently closed, so we are holding to OPEN it
            if (currentOpenCooldown > 0f) return; // Still in cooldown from recently closing!

            currentOpenHold += Time.deltaTime;
            if (currentOpenHold >= openHoldTime)
            {
                currentAngle = openAngle;
                ApplyRotation();
                Debug.Log("DOOR SLAMMED OPEN!");
                OnDoorFullyOpened?.Invoke();
                
                currentOpenHold = 0f;
                currentCloseCooldown = closeCooldownTime; // Trigger cooldown so they can't instantly close it
            }
        }
        else
        {
            // Door is already open or partially open, so holding closes it
            if (currentCloseCooldown > 0f) return; // Still in cooldown!

            bool wasClosedBeforeFrame = IsDoorClosed;

            currentAngle = Mathf.MoveTowards(currentAngle, closedAngle, closeSpeed * Time.deltaTime);
            ApplyRotation();

            // Only trigger the event if we JUST hit the threshold this exact frame
            if (!wasClosedBeforeFrame && IsDoorClosed)
            {
                Debug.Log("Door is fully closed!");
                OnDoorFullyClosed?.Invoke();
                
                // Trigger cooldown so they can't instantly open it again
                currentOpenCooldown = openCooldownTime; 
            }
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