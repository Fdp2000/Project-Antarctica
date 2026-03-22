using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Outline))]
public class WinchController : MonoBehaviour
{
    public event Action OnDoorFullyOpened;
    public event Action OnDoorFullyClosed;
    public event Action OnDoorStartedClosing;

    [Header("Audio System (Sources)")]
    [Tooltip("Attach an AudioSource here for the looping ratchet sounds.")]
    public AudioSource loopSource;
    [Tooltip("Attach a second AudioSource here for the explosive one-shot impacts.")]
    public AudioSource impactSource;

    [Header("Audio System (Clips)")]
    public AudioClip winchLoopNormal;
    public AudioClip doorSwingSound;
    public AudioClip doorSlamShut;
    public AudioClip doorSnowImpact;

    [Header("Audio Tuning")]
    public float maxPitch = 1.3f;
    public float minPitch = 0.8f;
    public float audioFadeSpeed = 8f;

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
            else currentOutsideTime = 0f;
        }
        else currentOutsideTime = 0f;

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

                // --- AUDIO: SNOW IMPACT ---
                if (impactSource != null && doorSnowImpact != null)
                {
                    impactSource.PlayOneShot(doorSnowImpact);
                }
            }
        }

        UpdateAudio();
    }

    private void UpdateAudio()
    {
        if (loopSource == null) return;

        if (isStruggling)
        {
            // The Clutch Controller handles the terrifying clip during struggle, 
            // we just make sure the volume stays at 100% while it fights.
            loopSource.volume = Mathf.Lerp(loopSource.volume, 1f, Time.deltaTime * audioFadeSpeed);
            return;
        }

        if (isSlamming)
        {
            loopSource.volume = Mathf.Lerp(loopSource.volume, 0f, Time.deltaTime * 15f);
            return;
        }

        if (isAutoOpening)
        {
            if (loopSource.clip != doorSwingSound)
            {
                loopSource.clip = doorSwingSound;
                loopSource.Play();
            }
            loopSource.volume = Mathf.Lerp(loopSource.volume, 1f, Time.deltaTime * audioFadeSpeed);
            loopSource.pitch = 1f;
        }
        else if (IsBeingHeld && !IsDoorClosed)
        {
            if (loopSource.clip != winchLoopNormal)
            {
                loopSource.clip = winchLoopNormal;
                loopSource.Play();
            }
            loopSource.volume = Mathf.Lerp(loopSource.volume, 1f, Time.deltaTime * audioFadeSpeed);

            // Dynamically pitch the sound up as the door closes faster
            float speedPercent = Mathf.InverseLerp(closeSpeedMin, closeSpeedMax, currentDynamicCloseSpeed);
            loopSource.pitch = Mathf.Lerp(minPitch, maxPitch, speedPercent);
        }
        else
        {
            // Player let go, slowly fade the ratchet out rather than cutting it instantly
            loopSource.volume = Mathf.Lerp(loopSource.volume, 0f, Time.deltaTime * audioFadeSpeed);
        }
    }

    public void InteractWinch()
    {
        if (doorHinge == null || isAutoOpening || isSlamming) return;
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

            if (wasFullyOpenBeforeFrame && !IsDoorOpen) OnDoorStartedClosing?.Invoke();
        }
    }

    public void ForceSlamShut()
    {
        if (!isSlamming) StartCoroutine(DoorSlamAndLockRoutine());
    }

    private void SyncMechanics()
    {
        if (doorHinge != null) doorHinge.localRotation = Quaternion.Euler(currentAngle, 0, 0);
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

        // --- AUDIO: DOOR SLAM SHUT ---
        if (impactSource != null && doorSlamShut != null)
        {
            impactSource.PlayOneShot(doorSlamShut);
        }

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