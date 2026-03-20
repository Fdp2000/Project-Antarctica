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
    public Transform monsterHead;
    public float playerCameraHeightOffset = 1.9f;

    [Header("Jumpscare Studio (Anti-Clip)")]
    public Camera jumpscareCamera;
    public Transform jumpscareStudioMonsterNode;

    [Header("Ambush Nodes")]
    public Transform shadowSpawnNode;
    public Transform doorwayLeapNode;

    // Called by the MonsterDirector when the timer hits zero
    public void ExecuteJumpscare(MonsterDirector.StrikeType strikeType, Transform monsterTransform, Transform rampEntryTarget)
    {
        StartCoroutine(JumpscareRoutine(strikeType, monsterTransform, rampEntryTarget));
    }

    private IEnumerator JumpscareRoutine(MonsterDirector.StrikeType activeStrikeType, Transform monsterTransform, Transform rampEntryTarget)
    {
        // --- THE FIX: Guarantee the monster is visible for the jumpscare! ---
        if (monsterTransform != null) monsterTransform.gameObject.SetActive(true);

        // 1. THE LOCK
        if (fpsController != null) fpsController.enabled = false;

        Quaternion startCamRot = playerCamera.rotation;
        Vector3 startLeapPos = rampEntryTarget.position;

        // 2. THE SNAP / BREACH (Getting into position)
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
        else // Normal Snap
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

        // 3. THE LEAP (Only for Ambush and Normal)
        if (activeStrikeType == MonsterDirector.StrikeType.Normal || activeStrikeType == MonsterDirector.StrikeType.Ambush)
        {
            float elapsed = 0f;

            // Calculate where the leap ends based on camera position
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

        // 4. THE JUMPSCARE STUDIO CUT (Anti-Clip Zoom & Scream)
        if (jumpscareCamera != null && jumpscareStudioMonsterNode != null && monsterTransform != null)
        {
            // Teleport monster to the hidden studio
            monsterTransform.position = jumpscareStudioMonsterNode.position;
            monsterTransform.rotation = jumpscareStudioMonsterNode.rotation;

            // Switch cameras
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
        }

        // 5. THE DEATH
        TriggerPlayerDeath();
    }
    private void TriggerPlayerDeath()
    {
        Debug.Log("<color=black><b>[ BLACK SCREEN - TRIGGERING DEATH SCREEN METHODS ]</b></color>");
        // TODO: Game Over Logic
    }
}