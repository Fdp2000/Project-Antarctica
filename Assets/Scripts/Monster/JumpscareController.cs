using UnityEngine;
using System.Collections;

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
    public float leapArcHeight = 1.2f; [Header("Stalk Behind Settings (50/50 Loss)")]
    public float stalkDuration = 4.0f;
    public float stalkDistance = 1.5f; [Tooltip("How much the player has to look at the monster before it strikes (1 = perfectly centered, 0.5 = edge of screen).")]
    public float stalkLookThreshold = 0.6f;
    public AudioSource monsterAudioSource;
    public AudioClip breathingSound;
    public BoxCollider stalkCollider; // Solid wall behind the player![Header("Jumpscare Execution (The Face Zoom)")]
    public AudioSource jumpscareAudioSource;
    public AudioClip jumpscareScream;
    public float jumpscareZoomDuration = 0.6f;
    public float targetZoomFOV = 30f;
    public float jumpscareHoldDuration = 1.2f;

    public Transform monsterHead;
    public float playerCameraHeightOffset = 1.9f;

    [Header("Jumpscare Studio (Anti-Clip)")]
    public Camera jumpscareCamera;
    public Transform jumpscareStudioMonsterNode;

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

        // 4. THE JUMPSCARE STUDIO CUT (Both Normal and Stalking funnels down into this!)
        if (jumpscareCamera != null && jumpscareStudioMonsterNode != null && monsterTransform != null)
        {
            monsterTransform.position = jumpscareStudioMonsterNode.position;
            monsterTransform.rotation = jumpscareStudioMonsterNode.rotation;

            if (playerCameraLens != null) playerCameraLens.gameObject.SetActive(false);
            jumpscareCamera.gameObject.SetActive(true);

            if (jumpscareAudioSource != null && jumpscareScream != null)
            {
                jumpscareAudioSource.PlayOneShot(jumpscareScream);
            }

            float elapsed = 0f;
            float startFOV = jumpscareCamera.fieldOfView;

            while (elapsed < jumpscareZoomDuration)
            {
                elapsed += Time.deltaTime;
                jumpscareCamera.fieldOfView = Mathf.Lerp(startFOV, targetZoomFOV, elapsed / jumpscareZoomDuration);

                if (monsterHead != null)
                {
                    jumpscareCamera.transform.rotation = Quaternion.LookRotation(monsterHead.position - jumpscareCamera.transform.position);
                }
                yield return null;
            }

            yield return new WaitForSeconds(jumpscareHoldDuration);
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
        if (stalkCollider != null) stalkCollider.enabled = false; // Reset collision failsafe
    }
}