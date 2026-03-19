using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Outline))]
public class WinchController : MonoBehaviour
{
    public event Action OnDoorFullyOpened;
    public event Action OnDoorFullyClosed;
    public event Action OnDoorStartedClosing;

    [Header("Door Settings")]
    public Transform doorHinge;
    public float openAngle = -211f;
    public float closedAngle = -90f;

    [Header("Valve Visuals (Perfect Clamping)")]
    [Tooltip("Drag your actual Valve Mesh here (not the empty parent collider)")]
    public Transform valveMesh;
    [Tooltip("Which axis the valve spins on (usually Forward, Up, or Right)")]
    public Vector3 valveRotationAxis = Vector3.forward;

    // --- NEW: Replaced arbitrary angle with full 360-degree spins ---
    [Tooltip("How many FULL 360-degree turns the valve makes. Guarantees Open and Closed look identical.")]
    public int valveFullSpins = 3;

    [Tooltip("The sudden snap rotation applied to the valve when the door slams shut.")]
    public float valveLockAngle = 50f;

    [Header("Animations (Violent Open / Heavy Close)")]
    public float openSlamSpeed = 350f;
    public float closeSpeedMin = 5f;
    public float closeSpeedMax = 40f;
    public float closeAcceleration = 20f;
    [Tooltip("The percentage (0.0 to 1.0) of remaining rotation before the door slams shut.")]
    public float slamThresholdPercent = 0.07f;
    [Tooltip("How long the door slam and lock twist takes (in seconds).")]
    public float slamDuration = 0.15f;

    [Header("Interaction Timers")]
    public float openHoldTime = 0.25f;
    public float closeCooldownTime = 1.2f;
    public float openCooldownTime = 2.0f;

    private float currentAngle;
    private float currentDynamicCloseSpeed;
    private float currentOpenHold = 0f;
    private float currentCloseCooldown = 0f;
    private float currentOpenCooldown = 0f;

    private bool isBeingHeldThisFrame = false;
    private bool isAutoOpening = false;
    private bool isSlamming = false;

    private Quaternion baseValveRotation;
    private float currentValveSpin = 0f;

    public bool IsDoorOpen => Mathf.Abs(currentAngle - openAngle) < 0.1f;
    public bool IsDoorClosed => Mathf.Abs(currentAngle - closedAngle) < 0.1f;

    public float CurrentAngle => currentAngle;

    void Start()
    {
        Outline outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        if (doorHinge == null) Debug.LogError("CRITICAL: Drag the Door_Hinge into the Inspector slot.");
        if (valveMesh != null) baseValveRotation = valveMesh.localRotation;

        currentAngle = openAngle;
        currentDynamicCloseSpeed = closeSpeedMin;
        SyncMechanics();
    }

    void Update()
    {
        if (currentCloseCooldown > 0f) currentCloseCooldown -= Time.deltaTime;
        if (currentOpenCooldown > 0f) currentOpenCooldown -= Time.deltaTime;

        if (!isBeingHeldThisFrame && !isSlamming)
        {
            currentOpenHold = 0f;
            currentDynamicCloseSpeed = closeSpeedMin;
        }

        // --- THE VIOLENT OPENING ANIMATION ---
        if (isAutoOpening && !isSlamming)
        {
            currentAngle = Mathf.MoveTowards(currentAngle, openAngle, openSlamSpeed * Time.deltaTime);
            SyncMechanics();

            if (Mathf.Abs(currentAngle - openAngle) < 0.1f)
            {
                currentAngle = openAngle;
                isAutoOpening = false;
                currentCloseCooldown = closeCooldownTime;

                SyncMechanics();

                Debug.Log("DOOR SLAMMED OPEN!");
                // TODO: AUDIO - Play a violent metallic slam/thud sound here
                OnDoorFullyOpened?.Invoke();
            }
        }

        isBeingHeldThisFrame = false;
    }

    public void InteractWinch()
    {
        if (doorHinge == null || isAutoOpening || isSlamming) return;

        isBeingHeldThisFrame = true;

        if (IsDoorClosed)
        {
            // --- TRIGGERING THE OPEN ---
            if (currentOpenCooldown > 0f) return;

            currentOpenHold += Time.deltaTime;

            if (currentOpenHold >= openHoldTime)
            {
                isAutoOpening = true;
                currentOpenHold = 0f;

                // TODO: AUDIO - Play a heavy mechanical "clunk/unlock" sound here
            }
        }
        else
        {
            // --- THE VARIABLE CLOSING ---
            if (currentCloseCooldown > 0f) return;

            bool wasFullyOpenBeforeFrame = IsDoorOpen;
            bool wasClosedBeforeFrame = IsDoorClosed;

            float totalTravel = Mathf.Abs(openAngle - closedAngle);
            float remainingTravel = Mathf.Abs(currentAngle - closedAngle);
            float percentLeft = remainingTravel / totalTravel;

            // 2. Check for the slam threshold
            if (percentLeft <= slamThresholdPercent && percentLeft > 0f)
            {
                StartCoroutine(DoorSlamAndLockRoutine());
                return;
            }

            // 3. Normal closing logic
            currentDynamicCloseSpeed = Mathf.MoveTowards(currentDynamicCloseSpeed, closeSpeedMax, closeAcceleration * Time.deltaTime);
            currentAngle = Mathf.MoveTowards(currentAngle, closedAngle, currentDynamicCloseSpeed * Time.deltaTime);

            SyncMechanics();

            if (wasFullyOpenBeforeFrame && !IsDoorOpen)
            {
                // TODO: AUDIO - Play a creaking hinge sound as it lifts off the snow
                OnDoorStartedClosing?.Invoke();
            }
        }
    }

    private void SyncMechanics()
    {
        if (doorHinge != null)
        {
            doorHinge.localRotation = Quaternion.Euler(currentAngle, 0, 0);
        }

        if (valveMesh != null)
        {
            // Calculate the exact rotation needed so that when the lock angle is added later, it hits a perfect 360 multiple.
            float totalTargetSpin = valveFullSpins * 360f;
            float baseCloseSpin = totalTargetSpin - valveLockAngle;

            float t = Mathf.InverseLerp(openAngle, closedAngle, currentAngle);
            currentValveSpin = Mathf.Lerp(0f, baseCloseSpin, t);

            valveMesh.localRotation = baseValveRotation * Quaternion.AngleAxis(currentValveSpin, valveRotationAxis);
        }
    }

    private IEnumerator DoorSlamAndLockRoutine()
    {
        isSlamming = true;
        float elapsed = 0f;

        float startDoorAngle = currentAngle;
        float startValveSpin = currentValveSpin;

        // Target is the perfect 360-degree multiple
        float targetValveSpin = valveFullSpins * 360f;

        while (elapsed < slamDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / slamDuration;

            float curve = Mathf.Sin(percent * Mathf.PI * 0.5f);

            currentAngle = Mathf.Lerp(startDoorAngle, closedAngle, curve);
            currentValveSpin = Mathf.Lerp(startValveSpin, targetValveSpin, curve);

            if (doorHinge != null) doorHinge.localRotation = Quaternion.Euler(currentAngle, 0, 0);
            if (valveMesh != null) valveMesh.localRotation = baseValveRotation * Quaternion.AngleAxis(currentValveSpin, valveRotationAxis);

            yield return null;
        }

        // Force exact final state 
        currentAngle = closedAngle;
        if (doorHinge != null) doorHinge.localRotation = Quaternion.Euler(currentAngle, 0, 0);
        if (valveMesh != null) valveMesh.localRotation = baseValveRotation * Quaternion.AngleAxis(targetValveSpin, valveRotationAxis);

        Debug.Log("DOOR SLAMMED SHUT & LOCKED!");
        // TODO: AUDIO - Play a heavy metal door slam & lock clatter sound here
        OnDoorFullyClosed?.Invoke();

        currentOpenCooldown = openCooldownTime;
        isSlamming = false;
    }
}