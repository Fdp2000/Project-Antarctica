using UnityEngine;

public class ScienceMinigame : MonoBehaviour
{
    public SimpleFPSController player;

    [Header("The Screen Setup")]
    public Transform screenOrigin;
    public LineRenderer targetLine;
    public LineRenderer playerLine;

    // 1. INCREASED RESOLUTION: Changed from 100 to 400 so squished waves stay smooth
    public int numPoints = 400;
    public float waveWidth = 1.0f;

    [Header("Visual Game Feel Tweaks")]
    // 2. THE SQUISH: This multiplies the frequency visually so you see WAY more of the wave
    public float visualDensity = 10f;
    // 3. THE POP: Pushes the line slightly away from the screen mesh so it doesn't clip
    public float zOffset = 0.02f;
    // Makes the paper look like it's feeding faster
    public float scrollSpeed = 5f;

    [Header("The Earth's Signal (Target)")]
    public float targetAmplitude = 0.15f;
    public float targetFrequency = 5.0f;
    public float targetPhase = 3.0f;

    [Header("The Machine Knobs (Player)")]
    public float playerAmplitude = 0.1f;
    public float playerFrequency = 1.0f;
    public float playerPhase = 0.0f;

    [Header("Minigame Settings")]
    public float knobSpeed = 2.5f;
    public float matchTolerance = 0.4f;
    public float downloadSpeed = 20f;

    [Header("Progress")]
    [Range(0, 100)]
    public float dataDownloaded = 0f;
    public bool isCompleted = false;

    void Start()
    {
        if (targetLine) targetLine.positionCount = numPoints;
        if (playerLine) playerLine.positionCount = numPoints;

        if (targetLine) targetLine.useWorldSpace = true;
        if (playerLine) playerLine.useWorldSpace = true;
    }

    void Update()
    {
        if (player != null && player.isDoingScience && !isCompleted)
        {
            HandleInputs();

            DrawWave(targetLine, targetAmplitude, targetFrequency, targetPhase);
            DrawWave(playerLine, playerAmplitude, playerFrequency, playerPhase);

            CheckSync();
        }
        else
        {
            if (targetLine) targetLine.enabled = false;
            if (playerLine) playerLine.enabled = false;
        }
    }

    void HandleInputs()
    {
        if (targetLine) targetLine.enabled = true;
        if (playerLine) playerLine.enabled = true;

        if (Input.GetKey(KeyCode.W)) playerAmplitude += knobSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) playerAmplitude -= knobSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.Q)) playerFrequency += knobSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.A)) playerFrequency -= knobSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.R)) playerPhase += knobSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.F)) playerPhase -= knobSpeed * Time.deltaTime;

        playerAmplitude = Mathf.Clamp(playerAmplitude, 0.05f, 0.4f);
        playerFrequency = Mathf.Clamp(playerFrequency, 1f, 10f);
        playerPhase = Mathf.Clamp(playerPhase, 0f, 10f);
    }

    void DrawWave(LineRenderer lr, float amp, float freq, float phase)
    {
        if (lr == null || screenOrigin == null) return;

        for (int i = 0; i < numPoints; i++)
        {
            float xProgress = (float)i / (numPoints - 1);
            float xPos = (xProgress * waveWidth) - (waveWidth / 2f);

            // Scroll speed is increased to match the denser waves
            float timeScroll = Time.time * scrollSpeed;

            // Multiply the frequency by visualDensity to pack more peaks onto the screen
            float yPos = amp * Mathf.Sin((freq * xPos * visualDensity) + phase + timeScroll);

            // Add the zOffset using screenOrigin.forward to push the line off the glass!
            Vector3 pointPosition = screenOrigin.position
                                  + (screenOrigin.right * xPos)
                                  + (screenOrigin.up * yPos)
                                  + (screenOrigin.forward * zOffset); // Try changing this to '-' if it pushes backwards into the monitor

            lr.SetPosition(i, pointPosition);
        }
    }

    void CheckSync()
    {
        float ampDiff = Mathf.Abs(targetAmplitude - playerAmplitude);
        float freqDiff = Mathf.Abs(targetFrequency - playerFrequency);
        float phaseDiff = Mathf.Abs(targetPhase - playerPhase);

        bool isSynced = (ampDiff <= matchTolerance && freqDiff <= matchTolerance && phaseDiff <= matchTolerance);

        if (isSynced)
        {
            dataDownloaded += downloadSpeed * Time.deltaTime;
            playerLine.startColor = Color.green;
            playerLine.endColor = Color.green;
        }
        else
        {
            dataDownloaded -= (downloadSpeed / 2f) * Time.deltaTime;
            playerLine.startColor = Color.black;
            playerLine.endColor = Color.black;
        }

        dataDownloaded = Mathf.Clamp(dataDownloaded, 0f, 100f);

        if (dataDownloaded >= 100f)
        {
            isCompleted = true;
        }
    }

    void OnGUI()
    {
        if (player == null || !player.isDoingScience) return;

        GUI.color = Color.black;
        GUI.Box(new Rect(10, 10, 300, 150), "");
        GUI.color = Color.white;

        if (isCompleted)
        {
            GUI.color = Color.green;
            GUI.skin.label.fontSize = 24;
            GUI.Label(new Rect(20, 60, 280, 50), "DATA DECRYPTED.\nSEQUENCE COMPLETE.");
            GUI.skin.label.fontSize = 12;
            return;
        }

        GUI.Label(new Rect(20, 20, 280, 20), "Target: A:" + targetAmplitude.ToString("F2") + " F:" + targetFrequency.ToString("F1") + " P:" + targetPhase.ToString("F1"));
        GUI.Label(new Rect(20, 50, 280, 20), "Player: A:" + playerAmplitude.ToString("F2") + " F:" + playerFrequency.ToString("F1") + " P:" + playerPhase.ToString("F1"));

        GUI.color = (dataDownloaded > 0 && dataDownloaded < 100) ? Color.yellow : Color.white;
        GUI.Label(new Rect(20, 90, 280, 30), "DATA SYNC: " + dataDownloaded.ToString("F0") + "%");
    }
}