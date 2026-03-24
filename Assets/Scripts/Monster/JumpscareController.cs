using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ==========================================
// --- THE SCENARIO DATA CONTAINER ---
// ==========================================
[System.Serializable]
public class JumpscareScenario
{
    public string scenarioName = "New Jumpscare";

    [Header("Studio Placements")]
    public Transform monsterStartNode;
    public Transform cameraNode;

    [Header("Lighting Nodes")]
    [Tooltip("Add as many empty Transforms here as you need for this specific scenario.")]
    public List<Transform> spotlightNodes = new List<Transform>();

    [Header("Animation & Timing")]
    public string animationTriggerBool;
    [Tooltip("How fast the animation plays to fit within the total duration.")]
    public float animationSpeedMultiplier = 1.0f;
    [Tooltip("The total time before cutting to the black death screen.")]
    public float totalJumpscareDuration = 1.5f;

    [Header("Camera FX")]
    public float zoomDuration = 0.5f;
    public float targetZoomFOV = 35f;

    [Header("Audio (The Vocal)")]
    [Tooltip("The unique 3D roar or bite sound for this specific monster animation.")]
    public AudioClip screamClip;
    [Range(0f, 1f)] public float screamVolume = 1.0f;
}

public class JumpscareController : MonoBehaviour
{
    [Header("Jumpscare Settings")]
    public Transform playerCamera;
    public Camera playerCameraLens;
    public MonoBehaviour fpsController;
    public Animator monsterAnimator;
    public float cameraSnapDuration = 0.15f;
    public float ambushBreachDuration = 0.25f;
    public float monsterLeapDuration = 0.3f;
    public float leapArcHeight = 1.2f;

    [Header("Stalk Behind Settings (50/50 Loss)")]
    public float stalkDuration = 4.0f;
    public float stalkDistance = 1.5f;
    [Tooltip("How much the player has to look at the monster before it strikes (1 = perfectly centered, 0.5 = edge of screen).")]
    public float stalkLookThreshold = 0.6f;
    public AudioSource monsterAudioSource;
    public AudioClip breathingSound;
    public BoxCollider stalkCollider; // Solid wall behind the player!

    [Header("Jumpscare Execution (The Face Zoom)")]
    public AudioSource jumpscareAudioSource;
    public AudioClip jumpscareScream;
    public float jumpscareZoomDuration = 0.6f;
    public float targetZoomFOV = 30f;
    public float jumpscareHoldDuration = 1.2f;

    public Transform monsterHead;
    public float playerCameraHeightOffset = 1.9f;

    // ==========================================
    // --- NEW: JUMPSCARE STUDIO SETTINGS ---
    // ==========================================
    [Header("Jumpscare Studio (The Puppet System)")]
    public Camera jumpscareCamera;
    public Transform jumpscareStudioMonsterNode; // The base puppet wrapper
    public Animator studioMonsterAnimator; // The Puppet's specific animator
    public AudioSource studioMonsterAudioSource; // The Puppet's mouth audio
    public List<Light> studioSpotlights = new List<Light>();

    [Header("Universal Audio (The Stinger)")]
    public AudioSource universalStingerSource;
    public AudioClip universalStingerClip;
    [Range(0f, 1f)] public float stingerVolume = 1.0f;

    [Header("The Scenarios")]
    [Tooltip("Set to -1 for random. Set to 0, 1, 2, etc., to force a specific scenario for testing.")]
    public int debugForceScenarioIndex = -1;
    public List<JumpscareScenario> scenarios = new List<JumpscareScenario>();
    // ==========================================

    [Header("Ambush Nodes")]
    public Transform shadowSpawnNode;
    public Transform doorwayLeapNode;

    public void ExecuteJumpscare(MonsterDirector.StrikeType strikeType, Transform monsterTransform, Transform rampEntryTarget)
    {
        // Don't trigger the leap instantly if we are just stalking!
        if (strikeType != MonsterDirector.StrikeType.StalkBehind && monsterAnimator != null)
        {
            monsterAnimator.SetBool("isLeaping", true);
        }

        StartCoroutine(JumpscareRoutine(strikeType, monsterTransform, rampEntryTarget));
    }

    private IEnumerator JumpscareRoutine(MonsterDirector.StrikeType activeStrikeType, Transform monsterTransform, Transform rampEntryTarget)
    {
        if (monsterTransform != null) monsterTransform.gameObject.SetActive(true);

        if (activeStrikeType == MonsterDirector.StrikeType.StalkBehind)
        {
            // --- 1. THE STALK SETUP ---
            Vector3 flatForward = playerCamera.forward;
            flatForward.y = 0;
            flatForward.Normalize();

            // Spawn directly behind the player's back
            Vector3 spawnPos = playerCamera.position - (flatForward * stalkDistance);
            spawnPos.y = playerCamera.position.y - playerCameraHeightOffset;

            if (monsterTransform != null)
            {
                monsterTransform.position = spawnPos;
                Vector3 flatLook = new Vector3(playerCamera.position.x, monsterTransform.position.y, playerCamera.position.z);
                monsterTransform.LookAt(flatLook);
            }

            // Set animations and audio
            if (monsterAnimator != null) monsterAnimator.SetBool("isStalking", true);
            if (stalkCollider != null) stalkCollider.enabled = true;

            if (monsterAudioSource != null && breathingSound != null)
            {
                monsterAudioSource.clip = breathingSound;
                monsterAudioSource.loop = true;
                monsterAudioSource.Play();
            }

            // --- 2. THE WAITING ROOM ---
            float stalkElapsed = 0f;
            while (stalkElapsed < stalkDuration)
            {
                stalkElapsed += Time.deltaTime;
                if (monsterTransform != null)
                {
                    // Check if player looked at the monster
                    Vector3 dirToMonster = (monsterTransform.position - playerCamera.position).normalized;
                    if (Vector3.Dot(playerCamera.forward, dirToMonster) > stalkLookThreshold)
                    {
                        break; // Player turned around! End the stalk early.
                    }
                }
                yield return null;
            }

            // --- 3. THE TRIGGER ---
            if (monsterAudioSource != null) monsterAudioSource.Stop();
            if (stalkCollider != null) stalkCollider.enabled = false;
            if (fpsController != null) fpsController.enabled = false; // Lock the player now!

            // Fast snap to face
            Quaternion startRot = playerCamera.rotation;
            float snapElapsed = 0f;
            while (snapElapsed < cameraSnapDuration)
            {
                snapElapsed += Time.deltaTime;
                Vector3 targetHeadPos = monsterHead != null ? monsterHead.position : monsterTransform.position + (Vector3.up * 2.2f);
                playerCamera.rotation = Quaternion.Slerp(startRot, Quaternion.LookRotation(targetHeadPos - playerCamera.position), snapElapsed / cameraSnapDuration);
                yield return null;
            }

            // Note: It naturally flows straight down into Phase 4 (The Jumpscare Studio Cut) from here!
        }
        else
        {
            // --- NORMAL JUMPSCARE LOGIC ---
            // 1. THE LOCK
            if (fpsController != null) fpsController.enabled = false;

            Quaternion startCamRot = playerCamera.rotation;
            Vector3 startLeapPos = rampEntryTarget.position;

            // 2. THE SNAP / BREACH 
            if (activeStrikeType == MonsterDirector.StrikeType.FogStrike || activeStrikeType == MonsterDirector.StrikeType.PointBlank)
            {
                if (monsterTransform != null)
                {
                    Vector3 instantSpawnPos = playerCamera.position + (playerCamera.forward * 1.5f);
                    instantSpawnPos.y = playerCamera.position.y - playerCameraHeightOffset;
                    monsterTransform.position = instantSpawnPos;
                    Vector3 flatLook = new Vector3(playerCamera.position.x, monsterTransform.position.y, playerCamera.position.z);
                    monsterTransform.LookAt(flatLook);
                }

                float elapsed = 0f;
                while (elapsed < cameraSnapDuration)
                {
                    elapsed += Time.deltaTime;
                    Vector3 targetHeadPos = monsterHead != null ? monsterHead.position : monsterTransform.position + (Vector3.up * 2.2f);
                    playerCamera.rotation = Quaternion.Slerp(startCamRot, Quaternion.LookRotation(targetHeadPos - playerCamera.position), elapsed / cameraSnapDuration);
                    yield return null;
                }
            }
            else if (activeStrikeType == MonsterDirector.StrikeType.Ambush && shadowSpawnNode != null && doorwayLeapNode != null)
            {
                if (monsterTransform != null) monsterTransform.position = shadowSpawnNode.position;

                float elapsed = 0f;
                while (elapsed < ambushBreachDuration)
                {
                    elapsed += Time.deltaTime;
                    float percent = elapsed / ambushBreachDuration;

                    monsterTransform.position = Vector3.Lerp(shadowSpawnNode.position, doorwayLeapNode.position, percent);
                    Vector3 flatLook = new Vector3(doorwayLeapNode.position.x, monsterTransform.position.y, doorwayLeapNode.position.z);
                    monsterTransform.LookAt(flatLook);

                    Vector3 directionToDoorway = (doorwayLeapNode.position + (Vector3.up * 1.5f)) - playerCamera.position;
                    playerCamera.rotation = Quaternion.Slerp(startCamRot, Quaternion.LookRotation(directionToDoorway), percent);

                    yield return null;
                }
                startLeapPos = doorwayLeapNode.position;
            }
            else
            {
                if (monsterTransform != null) monsterTransform.position = rampEntryTarget.position;

                float elapsed = 0f;
                while (elapsed < cameraSnapDuration)
                {
                    elapsed += Time.deltaTime;
                    Vector3 targetHeadPos = monsterHead != null ? monsterHead.position : monsterTransform.position + (Vector3.up * 2.2f);
                    playerCamera.rotation = Quaternion.Slerp(startCamRot, Quaternion.LookRotation(targetHeadPos - playerCamera.position), elapsed / cameraSnapDuration);
                    yield return null;
                }
            }

            // 3. THE LEAP 
            if (activeStrikeType == MonsterDirector.StrikeType.Normal || activeStrikeType == MonsterDirector.StrikeType.Ambush)
            {
                float elapsed = 0f;
                Vector3 finalFacePosition = playerCamera.position + (playerCamera.forward * 1.2f);
                finalFacePosition.y = playerCamera.position.y - playerCameraHeightOffset;

                while (elapsed < monsterLeapDuration)
                {
                    elapsed += Time.deltaTime;
                    float percent = elapsed / monsterLeapDuration;
                    float currentHeight = Mathf.Sin(percent * Mathf.PI) * leapArcHeight;
                    Vector3 currentPos = Vector3.Lerp(startLeapPos, finalFacePosition, percent);
                    currentPos.y += currentHeight;

                    if (monsterTransform != null)
                    {
                        monsterTransform.position = currentPos;
                        Vector3 flatLook = new Vector3(playerCamera.position.x, monsterTransform.position.y, playerCamera.position.z);
                        monsterTransform.LookAt(flatLook);
                    }
                    yield return null;
                }
            }
        }

        // ==========================================
        // --- 4. THE JUMPSCARE STUDIO CUT ---
        // ==========================================
        if (scenarios.Count > 0 && jumpscareCamera != null && playerCameraLens != null)
        {
            // Turn off the gameplay camera, turn on the studio camera
            playerCameraLens.gameObject.SetActive(false);
            jumpscareCamera.gameObject.SetActive(true);

            // Hide the gameplay monster so it doesn't overlap with the puppet
            if (monsterTransform != null) monsterTransform.gameObject.SetActive(false);

            // Pick the Scenario
            int pickedIndex = debugForceScenarioIndex;
            if (pickedIndex < 0 || pickedIndex >= scenarios.Count)
            {
                pickedIndex = Random.Range(0, scenarios.Count);
            }
            JumpscareScenario activeScenario = scenarios[pickedIndex];
            Debug.Log($"<color=magenta>Triggering Jumpscare Scenario: {activeScenario.scenarioName}</color>");

            // Position the Puppet
            if (jumpscareStudioMonsterNode != null && activeScenario.monsterStartNode != null)
            {
                jumpscareStudioMonsterNode.position = activeScenario.monsterStartNode.position;
                jumpscareStudioMonsterNode.rotation = activeScenario.monsterStartNode.rotation;
                jumpscareStudioMonsterNode.gameObject.SetActive(true);
            }

            // Position the Studio Camera
            if (activeScenario.cameraNode != null)
            {
                jumpscareCamera.transform.position = activeScenario.cameraNode.position;
                jumpscareCamera.transform.rotation = activeScenario.cameraNode.rotation;
            }

            // Setup the Lights
            for (int i = 0; i < studioSpotlights.Count; i++)
            {
                if (studioSpotlights[i] == null) continue;

                if (i < activeScenario.spotlightNodes.Count && activeScenario.spotlightNodes[i] != null)
                {
                    studioSpotlights[i].transform.position = activeScenario.spotlightNodes[i].position;
                    studioSpotlights[i].transform.rotation = activeScenario.spotlightNodes[i].rotation;
                    studioSpotlights[i].enabled = true;
                }
                else
                {
                    studioSpotlights[i].enabled = false;
                }
            }

            // Play Both Audio Layers
            if (universalStingerSource != null && universalStingerClip != null)
            {
                universalStingerSource.PlayOneShot(universalStingerClip, stingerVolume);
            }

            if (studioMonsterAudioSource != null && activeScenario.screamClip != null)
            {
                studioMonsterAudioSource.PlayOneShot(activeScenario.screamClip, activeScenario.screamVolume);
            }

            // Trigger Animation
            if (studioMonsterAnimator != null && !string.IsNullOrEmpty(activeScenario.animationTriggerBool))
            {
                studioMonsterAnimator.speed = activeScenario.animationSpeedMultiplier;
                studioMonsterAnimator.SetBool(activeScenario.animationTriggerBool, true);
            }

            // Process Camera Zoom and Timer
            float elapsedZoom = 0f;
            float startFOV = jumpscareCamera.fieldOfView;
            float timeRunning = 0f;

            while (timeRunning < activeScenario.totalJumpscareDuration)
            {
                timeRunning += Time.deltaTime;

                if (elapsedZoom < activeScenario.zoomDuration)
                {
                    elapsedZoom += Time.deltaTime;
                    jumpscareCamera.fieldOfView = Mathf.Lerp(startFOV, activeScenario.targetZoomFOV, elapsedZoom / activeScenario.zoomDuration);
                }

                yield return null;
            }
        }
        else
        {
            Debug.LogWarning("Jumpscare Studio skipped: Missing Scenarios or Cameras!");
        }

        // 5. THE DEATH
        TriggerPlayerDeath();
    }

    private void TriggerPlayerDeath()
    {
        Debug.Log("<color=black><b>[ BLACK SCREEN - TRIGGERING DEATH SCREEN METHODS ]</b></color>");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerPlayerDeath();
        }
    }

    public void ResetJumpscareState()
    {
        if (jumpscareCamera != null) jumpscareCamera.gameObject.SetActive(false);
        if (playerCameraLens != null) playerCameraLens.gameObject.SetActive(true);
        if (stalkCollider != null) stalkCollider.enabled = false;

        if (jumpscareStudioMonsterNode != null) jumpscareStudioMonsterNode.gameObject.SetActive(false);
        if (studioMonsterAnimator != null)
        {
            studioMonsterAnimator.speed = 1.0f;
            foreach (var scenario in scenarios)
            {
                if (!string.IsNullOrEmpty(scenario.animationTriggerBool))
                {
                    studioMonsterAnimator.SetBool(scenario.animationTriggerBool, false);
                }
            }
        }

        foreach (Light l in studioSpotlights)
        {
            if (l != null) l.enabled = false;
        }
    }
}