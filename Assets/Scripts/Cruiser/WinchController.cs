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
    public Transform valveMesh;
    public Vector3 valveRotationAxis = Vector3.forward;
    public int valveFullSpins = 3;
    public float valveLockAngle = 50f;

    [Header("Animations (Violent Open / Heavy Close)")]
    public float openSlamSpeed = 350f;
    public float closeSpeedMin = 5f;
    public float closeSpeedMax = 40f;
    public float closeAcceleration = 20f;
    public float slamThresholdPercent = 0.07f;
    public float slamDuration = 0.15f;

    [Header("Interaction Timers")]
    public float openHoldTime = 0.25f;
    public float closeCooldownTime = 1.2f;
    public float openCooldownTime = 2.0f;

    [Header("Clutch State")]
    [Tooltip("If true, normal closing physics are suspended for the tug-of-war.")]
    public bool isStruggling = false;

    [Header("Environment Forces (Wind Slam)")]
    public bool isPlayerInside = true;
    public float autoOpenOutsideTimer = 4.0f;
    private float currentOutsideTime = 0f;

    private float currentAngle;
    private float currentDynamicCloseSpeed;
    private float currentOpenHold = 0f;
    private float currentCloseCooldown = 0f;
    private float currentOpenCooldown = 0f;

    private bool isAutoOpening = false;
    private bool isSlamming = false;

    // --- THE FIX: Restored the bulletproof input timer ---
    private float timeSinceLastInteract = 100f;
    public bool IsBeingHeld => timeSinceLastInteract <= 0.15f;

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
        // Tick our input timer up every frame
        timeSinceLastInteract += Time.deltaTime;

        if (currentCloseCooldown > 0f) currentCloseCooldown -= Time.deltaTime;
        if (currentOpenCooldown > 0f) currentOpenCooldown -= Time.deltaTime;

        if (!isPlayerInside && !IsDoorOpen && !isSlamming && !isStruggling && !isAutoOpening)
        {
            if (!IsBeingHeld)
            {
                currentOutsideTime += Time.deltaTime;
                if (currentOutsideTime >= autoOpenOutsideTimer)
                {
                    isAutoOpening = true;
                    currentOutsideTime = 0f;
                    Debug.Log("<color=orange>ENVIRONMENT: Wind slammed the unattended door open!</color>");
                }
            }
            else
            {
                currentOutsideTime = 0f;
            }
        }
        else
        {
            currentOutsideTime = 0f;
        }

        // Use the robust property here
        if (!IsBeingHeld && !isSlamming)
        {
            currentOpenHold = 0f;
            currentDynamicCloseSpeed = closeSpeedMin;
        }

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
                OnDoorFullyOpened?.Invoke();
            }
        }
    }

    public void InteractWinch()
    {
        if (doorHinge == null || isAutoOpening || isSlamming) return;

        // --- THE FIX: Lock the timer to 0 every time the player clicks ---
        timeSinceLastInteract = 0f;

        if (IsDoorClosed)
        {
            if (currentOpenCooldown > 0f) return;

            currentOpenHold += Time.deltaTime;

            if (currentOpenHold >= openHoldTime)
            {
                isAutoOpening = true;
                currentOpenHold = 0f;
            }
        }
        else
        {
            if (currentCloseCooldown > 0f) return;
            if (isStruggling) return;

            bool wasFullyOpenBeforeFrame = IsDoorOpen;

            float totalTravel = Mathf.Abs(openAngle - closedAngle);
            float remainingTravel = Mathf.Abs(currentAngle - closedAngle);
            float percentLeft = remainingTravel / totalTravel;

            if (percentLeft <= slamThresholdPercent && percentLeft > 0f)
            {
                StartCoroutine(DoorSlamAndLockRoutine());
                return;
            }

            currentDynamicCloseSpeed = Mathf.MoveTowards(currentDynamicCloseSpeed, closeSpeedMax, closeAcceleration * Time.deltaTime);
            currentAngle = Mathf.MoveTowards(currentAngle, closedAngle, currentDynamicCloseSpeed * Time.deltaTime);

            SyncMechanics();

            if (wasFullyOpenBeforeFrame && !IsDoorOpen)
            {
                OnDoorStartedClosing?.Invoke();
            }
        }
    }

    public void ForceSlamShut()
    {
        if (!isSlamming)
        {
            StartCoroutine(DoorSlamAndLockRoutine());
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

        currentAngle = closedAngle;
        if (doorHinge != null) doorHinge.localRotation = Quaternion.Euler(currentAngle, 0, 0);
        if (valveMesh != null) valveMesh.localRotation = baseValveRotation * Quaternion.AngleAxis(targetValveSpin, valveRotationAxis);

        Debug.Log("DOOR SLAMMED SHUT & LOCKED!");
        OnDoorFullyClosed?.Invoke();

        currentOpenCooldown = openCooldownTime;
        isSlamming = false;
        isStruggling = false;
    }

    public void SetStruggleAngle(float forcedAngle)
    {
        currentAngle = Mathf.Clamp(forcedAngle, openAngle, closedAngle);
        SyncMechanics();
    }
}