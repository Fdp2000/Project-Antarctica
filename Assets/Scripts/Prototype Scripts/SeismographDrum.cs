using UnityEngine;
using System.Collections.Generic;

public class SeismographDrum : MonoBehaviour
{
    [Header("Hardware References")]
    public Transform drumRoll;
    public Transform targetNeedle;
    public Transform playerNeedle;
    public LineRenderer targetInk;
    public LineRenderer playerInk;
    public Transform needleAnchor;

    [Header("Drum Mechanics")]
    public float drumSpinSpeed = 15f;
    public int maxInkPoints = 500;

    [Header("Player Dials")]
    public float playerAmplitude = 0.1f;
    public float playerFrequency = 1.0f;
    public float playerPhase = 0.0f; // This is the "Timing" adjustment dial

    [Header("Target Signal")]
    public float targetAmplitude = 0.15f;
    public float targetFrequency = 2.5f;
    public float targetPhase = 1.0f;

    [Header("Minigame Progress")]
    public float syncProgress = 0f;
    public bool isComplete = false;

    private List<Vector3> playerPoints = new List<Vector3>();
    private List<Vector3> targetPoints = new List<Vector3>();

    // Internal accumulators for smooth "analog" frequency changes
    private float pPhaseAccumulator = 0f;
    private float tPhaseAccumulator = 0f;

    void Update()
    {
        // 1. Physically spin the drum
        if (drumRoll) drumRoll.Rotate(-Vector3.up * drumSpinSpeed * Time.deltaTime);

        // 2. Smooth "Analog" Wave Math
        // We accumulate phase over time so frequency changes don't cause "jumps"
        pPhaseAccumulator += Time.deltaTime * playerFrequency;
        tPhaseAccumulator += Time.deltaTime * targetFrequency;

        float pY = playerAmplitude * Mathf.Sin(pPhaseAccumulator + playerPhase);
        float tY = targetAmplitude * Mathf.Sin(tPhaseAccumulator + targetPhase);

        // 3. Move needles based on the Anchor
        if (needleAnchor)
        {
            targetNeedle.position = needleAnchor.position + new Vector3(0, tY, 0);
            playerNeedle.position = needleAnchor.position + new Vector3(0, pY, -0.002f);
        }

        // 4. Record local points for the rolling paper effect
        playerPoints.Add(drumRoll.InverseTransformPoint(playerNeedle.position));
        targetPoints.Add(drumRoll.InverseTransformPoint(targetNeedle.position));

        if (playerPoints.Count > maxInkPoints) playerPoints.RemoveAt(0);
        if (targetPoints.Count > maxInkPoints) targetPoints.RemoveAt(0);

        playerInk.positionCount = playerPoints.Count;
        targetInk.positionCount = targetPoints.Count;
        playerInk.SetPositions(playerPoints.ToArray());
        targetInk.SetPositions(targetPoints.ToArray());

        if (!isComplete) CheckSync(pY, tY);
    }

    void CheckSync(float pY, float tY)
    {
        // We check if the PHYSICAL needles are currently overlapping
        float verticalDiff = Mathf.Abs(pY - tY);

        // If the needles are overlapping (synced in Amp, Freq, and Phase)
        if (verticalDiff < 0.02f && Mathf.Abs(playerFrequency - targetFrequency) < 0.1f)
        {
            syncProgress += Time.deltaTime * 20f;
            playerInk.startColor = Color.green;
            playerInk.endColor = Color.green;
        }
        else
        {
            syncProgress = Mathf.Max(0, syncProgress - Time.deltaTime * 10f);
            playerInk.startColor = Color.black;
            playerInk.endColor = Color.black;
        }

        if (syncProgress >= 100f)
        {
            isComplete = true;
            Debug.Log("MINIGAME SUCCESS: Signal Locked.");
        }
    }
}