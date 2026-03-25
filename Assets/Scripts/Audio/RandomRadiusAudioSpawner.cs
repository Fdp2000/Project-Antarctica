using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RandomRadiusAudioSpawner : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Needed to check if the player is currently inside the cabin.")]
    public MonsterDirector monsterDirector;
    [Tooltip("The player to center the spawn radius around.")]
    public Transform playerTransform;

    [Header("Timing")]
    [Tooltip("X = Minimum seconds, Y = Maximum seconds before the next sound plays.")]
    public Vector2 timeBetweenSounds = new Vector2(15f, 40f);

    [Header("Spawning")]
    [Tooltip("How far away from the player the sound will spawn.")]
    public float spawnRadius = 20f;
    [Tooltip("If true, the sound will spawn at the exact same height (Y) as the player.")]
    public bool matchPlayerHeight = true;

    [Header("Audio")]
    [Tooltip("Put a few different sounds here so it doesn't get repetitive!")]
    public AudioClip[] audioClips;
    [Range(0f, 1f)] public float volume = 1.0f;

    // --- Private Variables ---
    private AudioSource audioSource;
    private float timer;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Pick the first random countdown time
        ResetTimer();
    }

    void Update()
    {
        // 1. Tick down the timer
        timer -= Time.deltaTime;

        // 2. When the timer hits 0
        if (timer <= 0f)
        {
            // 3. Check the condition: Is the player in the cabin?
            if (monsterDirector != null && monsterDirector.isPlayerInCabin)
            {
                PlaySoundAtRandomEdge();
            }

            // 4. Start the timer over again (even if they weren't in the cabin, 
            // so it tries again later instead of firing instantly when they enter)
            ResetTimer();
        }
    }

    private void PlaySoundAtRandomEdge()
    {
        if (playerTransform == null || audioClips.Length == 0) return;

        // --- THE MATH: Find a random point on the edge of the circle ---
        float randomAngle = Random.Range(0f, 360f);

        // Convert the angle into a 3D direction (X and Z)
        Vector3 direction = new Vector3(Mathf.Sin(randomAngle), 0f, Mathf.Cos(randomAngle));

        // Multiply by the radius to push it out to the edge
        Vector3 spawnPosition = playerTransform.position + (direction * spawnRadius);

        // Keep it level with the player if requested
        if (matchPlayerHeight)
        {
            spawnPosition.y = playerTransform.position.y;
        }

        // Teleport this GameObject to the new spot
        transform.position = spawnPosition;

        // Pick a random clip from the array
        AudioClip clipToPlay = audioClips[Random.Range(0, audioClips.Length)];

        // Play the sound!
        audioSource.PlayOneShot(clipToPlay, volume);

        Debug.Log($"<color=gray>[Ambient Spawner] Played {clipToPlay.name} at {spawnRadius}m away.</color>");
    }

    private void ResetTimer()
    {
        timer = Random.Range(timeBetweenSounds.x, timeBetweenSounds.y);
    }
}