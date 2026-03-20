using UnityEngine;
using System.Collections;

public class JumpscareController : MonoBehaviour
{
    [Header("Jumpscare Settings")]
    public Transform playerCamera;
    public Camera playerCameraLens;
    public MonoBehaviour fpsController;
    public float cameraSnapDuration = 0.15f;
    public float ambushBreachDuration = 0.25f;
    public float monsterLeapDuration = 0.3f;
    public float leapArcHeight = 1.2f;

    [Header("Jumpscare Execution (The Face Zoom)")]
    public AudioSource jumpscareAudioSource;
    public AudioClip jumpscareScream;
    public float jumpscareZoomDuration = 0.6f;
    public float targetZoomFOV = 30f;

    // --- NEW: Controls how long you stare at the monster before death ---
    [Tooltip("How long the monster's face stays on screen after the zoom finishes before cutting to black.")]
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
        StartCoroutine(JumpscareRoutine(strikeType, monsterTransform, rampEntryTarget));
    }

    private IEnumerator JumpscareRoutine(MonsterDirector.StrikeType activeStrikeType, Transform monsterTransform, Transform rampEntryTarget)
    {
        if (monsterTransform != null) monsterTransform.gameObject.SetActive(true);

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

        // 4. THE JUMPSCARE STUDIO CUT 
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

            // --- THE FIX: Hold the terrifying face on screen before cutting to black! ---
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
    }
}