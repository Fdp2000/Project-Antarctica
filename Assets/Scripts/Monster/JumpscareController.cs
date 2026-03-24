using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class JumpscareScenario
{
    public string scenarioName = "New Jumpscare";

    [Header("Studio Placements")]
    public Transform monsterStartNode;
    public Transform cameraNode;

    [Header("Lighting Nodes")]
    public List<Transform> spotlightNodes = new List<Transform>();

    [Header("Animation & Timing")]
    public string animationTriggerBool;
    public float animationSpeedMultiplier = 1.0f;
    public float totalJumpscareDuration = 1.5f;

    [Header("Camera FX")]
    public float zoomDuration = 0.5f;
    public float targetZoomFOV = 35f;

    [Header("Audio (The Vocal)")]
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
    public float stalkLookThreshold = 0.6f;
    public AudioSource monsterAudioSource;
    public AudioClip breathingSound;
    public BoxCollider stalkCollider;

    [Header("Jumpscare Execution (The Face Zoom)")]
    public AudioSource jumpscareAudioSource;
    public AudioClip jumpscareScream;
    public float jumpscareZoomDuration = 0.6f;
    public float targetZoomFOV = 30f;
    public float jumpscareHoldDuration = 1.2f;

    public Transform monsterHead;
    public float playerCameraHeightOffset = 1.9f;

    [Header("Jumpscare Studio (The Puppet System)")]
    public Camera jumpscareCamera;
    public Transform jumpscareStudioMonsterNode;
    public Animator studioMonsterAnimator;
    public AudioSource studioMonsterAudioSource;
    public List<Light> studioSpotlights = new List<Light>();

    [Header("Universal Audio (The Stinger)")]
    public AudioSource universalStingerSource;
    public AudioClip universalStingerClip;
    [Range(0f, 1f)] public float stingerVolume = 1.0f;

    [Header("The Scenarios")]
    public int debugForceScenarioIndex = -1;
    public List<JumpscareScenario> scenarios = new List<JumpscareScenario>();

    [Header("Ambush Nodes")]
    public Transform shadowSpawnNode;
    public Transform doorwayLeapNode;

    public void ExecuteJumpscare(MonsterDirector.StrikeType strikeType, Transform monsterTransform, Transform rampEntryTarget)
    {
        // THE FIX: Removed the animation trigger from here so Unity doesn't ignore it!
        StartCoroutine(JumpscareRoutine(strikeType, monsterTransform, rampEntryTarget));
    }

    private IEnumerator JumpscareRoutine(MonsterDirector.StrikeType activeStrikeType, Transform monsterTransform, Transform rampEntryTarget)
    {
        // 1. Turn the monster back on FIRST
        if (monsterTransform != null) monsterTransform.gameObject.SetActive(true);

        // THE FIX: Now that the monster is active, trigger the Leap animation!
        if (activeStrikeType == MonsterDirector.StrikeType.Normal || activeStrikeType == MonsterDirector.StrikeType.Ambush)
        {
            if (monsterAnimator != null) monsterAnimator.SetBool("isLeaping", true);
        }

        if (activeStrikeType == MonsterDirector.StrikeType.StalkBehind)
        {
            // --- 1. THE STALK SETUP ---
            Vector3 flatForward = playerCamera.forward;
            flatForward.y = 0;
            flatForward.Normalize();

            Vector3 spawnPos = playerCamera.position - (flatForward * stalkDistance);
            spawnPos.y = fpsController.transform.position.y;

            if (monsterTransform != null)
            {
                monsterTransform.position = spawnPos;
                Vector3 flatLook = new Vector3(playerCamera.position.x, monsterTransform.position.y, playerCamera.position.z);
                monsterTransform.LookAt(flatLook);
            }

            if (monsterAnimator != null) monsterAnimator.SetBool("isStalking", true);
            if (stalkCollider != null) stalkCollider.enabled = true;

            if (monsterAudioSource != null && breathingSound != null)
            {
                monsterAudioSource.clip = breathingSound;
                monsterAudioSource.loop = true;
                monsterAudioSource.Play();
            }

            float stalkElapsed = 0f;
            while (stalkElapsed < stalkDuration)
            {
                stalkElapsed += Time.deltaTime;
                if (monsterTransform != null)
                {
                    Vector3 dirToMonster = (monsterTransform.position - playerCamera.position).normalized;
                    if (Vector3.Dot(playerCamera.forward, dirToMonster) > stalkLookThreshold) break;
                }
                yield return null;
            }

            if (monsterAudioSource != null) monsterAudioSource.Stop();
            if (stalkCollider != null) stalkCollider.enabled = false;
            if (fpsController != null) fpsController.enabled = false;

            Quaternion startRot = playerCamera.rotation;
            float snapElapsed = 0f;
            while (snapElapsed < cameraSnapDuration)
            {
                snapElapsed += Time.deltaTime;
                Vector3 targetHeadPos = monsterHead != null ? monsterHead.position : monsterTransform.position + (Vector3.up * 2.2f);
                playerCamera.rotation = Quaternion.Slerp(startRot, Quaternion.LookRotation(targetHeadPos - playerCamera.position), snapElapsed / cameraSnapDuration);
                yield return null;
            }
        }
        else
        {
            // --- NORMAL JUMPSCARE LOGIC (ALL FRONTAL STRIKES) ---
            if (fpsController != null) fpsController.enabled = false;

            Quaternion startCamRot = playerCamera.rotation;
            Vector3 startLeapPos = rampEntryTarget.position;

            // 2. THE SNAP / BREACH 
            if (activeStrikeType == MonsterDirector.StrikeType.FogStrike || activeStrikeType == MonsterDirector.StrikeType.PointBlank)
            {
                if (monsterTransform != null)
                {
                    // THE FIX: Reverted to 1.5f so they spawn directly in your face without leaping!
                    Vector3 spawnPos = playerCamera.position + (playerCamera.forward * 1.5f);
                    spawnPos.y = fpsController.transform.position.y;
                    monsterTransform.position = spawnPos;

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

            // 3. THE HEAT-SEEKING LEAP 
            // THE FIX: Explicitly locked behind Normal and Ambush strikes!
            if (activeStrikeType == MonsterDirector.StrikeType.Normal || activeStrikeType == MonsterDirector.StrikeType.Ambush)
            {
                float leapElapsed = 0f;

                Vector3 leapTargetPos = playerCamera.position + (playerCamera.forward * 1.0f);
                leapTargetPos.y = fpsController.transform.position.y;

                while (leapElapsed < monsterLeapDuration)
                {
                    leapElapsed += Time.deltaTime;
                    float percent = leapElapsed / monsterLeapDuration;
                    float currentHeight = Mathf.Sin(percent * Mathf.PI) * leapArcHeight;

                    Vector3 currentPos = Vector3.Lerp(startLeapPos, leapTargetPos, percent);
                    currentPos.y += currentHeight;

                    if (monsterTransform != null)
                    {
                        monsterTransform.position = currentPos;

                        Vector3 flatLook = new Vector3(playerCamera.position.x, monsterTransform.position.y, playerCamera.position.z);
                        monsterTransform.LookAt(flatLook);

                        if (monsterHead != null)
                        {
                            playerCamera.rotation = Quaternion.LookRotation(monsterHead.position - playerCamera.position);
                        }
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
            playerCameraLens.gameObject.SetActive(false);
            jumpscareCamera.gameObject.SetActive(true);

            if (monsterTransform != null) monsterTransform.gameObject.SetActive(false);

            int pickedIndex = debugForceScenarioIndex;
            if (pickedIndex < 0 || pickedIndex >= scenarios.Count)
            {
                pickedIndex = Random.Range(0, scenarios.Count);
            }
            JumpscareScenario activeScenario = scenarios[pickedIndex];
            Debug.Log($"<color=magenta>Triggering Jumpscare Scenario: {activeScenario.scenarioName}</color>");

            if (jumpscareStudioMonsterNode != null && activeScenario.monsterStartNode != null)
            {
                jumpscareStudioMonsterNode.position = activeScenario.monsterStartNode.position;
                jumpscareStudioMonsterNode.rotation = activeScenario.monsterStartNode.rotation;
                jumpscareStudioMonsterNode.gameObject.SetActive(true);
            }

            if (activeScenario.cameraNode != null)
            {
                jumpscareCamera.transform.position = activeScenario.cameraNode.position;
                jumpscareCamera.transform.rotation = activeScenario.cameraNode.rotation;
            }

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

            if (universalStingerSource != null && universalStingerClip != null)
            {
                universalStingerSource.PlayOneShot(universalStingerClip, stingerVolume);
            }

            if (studioMonsterAudioSource != null && activeScenario.screamClip != null)
            {
                studioMonsterAudioSource.PlayOneShot(activeScenario.screamClip, activeScenario.screamVolume);
            }

            if (studioMonsterAnimator != null && !string.IsNullOrEmpty(activeScenario.animationTriggerBool))
            {
                studioMonsterAnimator.speed = activeScenario.animationSpeedMultiplier;
                studioMonsterAnimator.SetBool(activeScenario.animationTriggerBool, true);
            }

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