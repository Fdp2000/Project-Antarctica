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
    public float stalkLookThreshold = 0.6f;
    public AudioSource monsterAudioSource;
    public AudioClip breathingSound;
    public BoxCollider stalkCollider;

    [Header("Jumpscare Studio (The Puppet System)")]
    public Camera jumpscareCamera;
    public Transform jumpscareStudioMonsterNode;
    public Animator studioMonsterAnimator;
    public AudioSource studioMonsterAudioSource;
    public List<Light> studioSpotlights = new List<Light>();

    // ==========================================
    // --- NEW: THE UNIVERSAL STINGER ---
    // ==========================================
    [Header("Universal Audio (The Stinger)")]
    [Tooltip("The 2D AudioSource attached to the Jumpscare Camera or Manager.")]
    public AudioSource universalStingerSource;
    [Tooltip("The massive cinematic BANG that plays on frame 1 of every scenario.")]
    public AudioClip universalStingerClip;
    [Range(0f, 1f)] public float stingerVolume = 1.0f;
    // ==========================================

    [Header("The Scenarios")]
    [Tooltip("Set to -1 for random. Set to 0, 1, 2, etc., to force a specific scenario for testing.")]
    public int debugForceScenarioIndex = -1;
    public List<JumpscareScenario> scenarios = new List<JumpscareScenario>();

    [Header("Ambush Nodes")]
    public Transform shadowSpawnNode;
    public Transform doorwayLeapNode;

    public void ExecuteJumpscare(MonsterDirector.StrikeType strikeType, Transform monsterPhys, Transform rampEntry)
    {
        StartCoroutine(JumpscareRoutine(strikeType, monsterPhys, rampEntry));
    }

    private IEnumerator JumpscareRoutine(MonsterDirector.StrikeType strikeType, Transform monsterPhys, Transform rampEntry)
    {
        Debug.Log($"<color=red><b>[ FATAL STRIKE INITIATED: {strikeType} ]</b></color>");

        if (fpsController != null) fpsController.enabled = false;

        // 1. STALK BEHIND FUNNEL
        if (strikeType == MonsterDirector.StrikeType.StalkBehind)
        {
            if (stalkCollider != null) stalkCollider.enabled = true;

            if (monsterPhys != null)
            {
                monsterPhys.gameObject.SetActive(true);
                Vector3 behindPos = playerCamera.position - (playerCamera.forward * stalkDistance);
                behindPos.y = rampEntry.position.y;
                monsterPhys.position = behindPos;
                monsterPhys.rotation = Quaternion.LookRotation(playerCamera.position - monsterPhys.position);
            }

            if (monsterAudioSource != null && breathingSound != null)
            {
                monsterAudioSource.clip = breathingSound;
                monsterAudioSource.loop = true;
                monsterAudioSource.Play();
            }

            float stalkTimer = 0f;
            bool playerLooked = false;

            while (stalkTimer < stalkDuration)
            {
                stalkTimer += Time.deltaTime;

                if (monsterPhys != null)
                {
                    Vector3 dirToMonster = (monsterPhys.position - playerCamera.position).normalized;
                    float dotProduct = Vector3.Dot(playerCamera.forward, dirToMonster);

                    if (dotProduct > stalkLookThreshold)
                    {
                        playerLooked = true;
                        break;
                    }
                }
                yield return null;
            }

            if (monsterAudioSource != null) monsterAudioSource.Stop();
        }
        else
        {
            // 2. THE CAMERA SNAP
            Vector3 startPos = playerCamera.position;
            Quaternion startRot = playerCamera.rotation;
            Vector3 targetPos = startPos;
            Quaternion targetRot = startRot;

            if (strikeType == MonsterDirector.StrikeType.FogStrike || strikeType == MonsterDirector.StrikeType.PointBlank)
            {
                if (monsterPhys != null) targetRot = Quaternion.LookRotation(monsterPhys.position - playerCamera.position);
            }
            else if (strikeType == MonsterDirector.StrikeType.Ambush || strikeType == MonsterDirector.StrikeType.Normal)
            {
                if (doorwayLeapNode != null) targetRot = Quaternion.LookRotation(doorwayLeapNode.position - playerCamera.position);
            }

            float elapsedSnap = 0f;
            while (elapsedSnap < cameraSnapDuration)
            {
                elapsedSnap += Time.deltaTime;
                playerCamera.rotation = Quaternion.Slerp(startRot, targetRot, elapsedSnap / cameraSnapDuration);
                yield return null;
            }

            // 3. THE LEAP 
            if (monsterAnimator != null) monsterAnimator.SetBool("isLeaping", true);

            if (strikeType == MonsterDirector.StrikeType.Ambush && doorwayLeapNode != null && shadowSpawnNode != null)
            {
                monsterPhys.position = shadowSpawnNode.position;
                float elapsedBreach = 0f;
                while (elapsedBreach < ambushBreachDuration)
                {
                    elapsedBreach += Time.deltaTime;
                    monsterPhys.position = Vector3.Lerp(shadowSpawnNode.position, doorwayLeapNode.position, elapsedBreach / ambushBreachDuration);
                    yield return null;
                }
            }

            if (monsterPhys != null && doorwayLeapNode != null)
            {
                Vector3 leapStartPos = monsterPhys.position;
                Vector3 leapTargetPos = playerCamera.position;

                float elapsedLeap = 0f;
                while (elapsedLeap < monsterLeapDuration)
                {
                    elapsedLeap += Time.deltaTime;
                    float t = elapsedLeap / monsterLeapDuration;
                    Vector3 currentPos = Vector3.Lerp(leapStartPos, leapTargetPos, t);
                    currentPos.y += Mathf.Sin(t * Mathf.PI) * leapArcHeight;
                    monsterPhys.position = currentPos;
                    yield return null;
                }
            }
        }

        // 4. THE JUMPSCARE STUDIO CUT
        if (scenarios.Count > 0 && jumpscareCamera != null && playerCameraLens != null)
        {
            playerCameraLens.gameObject.SetActive(false);
            jumpscareCamera.gameObject.SetActive(true);

            if (monsterPhys != null) monsterPhys.gameObject.SetActive(false);

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

            // --- NEW: Play BOTH Audio Layers ---
            if (universalStingerSource != null && universalStingerClip != null)
            {
                universalStingerSource.PlayOneShot(universalStingerClip, stingerVolume);
            }

            if (studioMonsterAudioSource != null && activeScenario.screamClip != null)
            {
                studioMonsterAudioSource.PlayOneShot(activeScenario.screamClip, activeScenario.screamVolume);
            }
            // -----------------------------------

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