using UnityEngine;

public class BeaconRandomizer : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The center point for the spawn radius (your vehicle).")]
    public Transform vehicleTransform;

    [Tooltip("Drag the Radio Beacons from your scene into this array.")]
    public RadioBeacon[] beaconsToMove;

    [Header("Spawn Settings")]
    [Tooltip("The maximum distance a beacon can spawn from the vehicle.")]
    public float maxSpawnRadius = 150f;

    [Tooltip("The minimum distance a beacon can spawn (so it doesn't spawn on top of you).")]
    public float minSpawnRadius = 30f;

    void Start()
    {
        if (vehicleTransform == null)
        {
            Debug.LogWarning("BeaconRandomizer: No vehicle transform assigned!");
            return;
        }

        RandomizeBeacons();
    }

    // The ContextMenu tag lets you right-click the script in the Inspector 
    // to test the randomization without having to press Play!
    [ContextMenu("Test Randomize Now")]
    public void RandomizeBeacons()
    {
        foreach (RadioBeacon beacon in beaconsToMove)
        {
            if (beacon == null) continue;

            // 1. Get a random 2D direction (X and Y)
            Vector2 randomDir = Random.insideUnitCircle.normalized;

            // 2. Pick a random distance between the minimum and the 150m maximum
            float randomDistance = Random.Range(minSpawnRadius, maxSpawnRadius);

            // 3. Convert that 2D circle into 3D world space (X and Z flat on the ground)
            Vector3 spawnOffset = new Vector3(randomDir.x * randomDistance, 0f, randomDir.y * randomDistance);

            // 4. Apply the offset to the vehicle's starting position
            Vector3 newPosition = vehicleTransform.position + spawnOffset;

            // 5. Keep the beacon's original Y (height) so it doesn't sink underground
            newPosition.y = beacon.transform.position.y;

            // Move the beacon
            beacon.transform.position = newPosition;
        }
    }

    // This draws the min and max spawn circles in the Scene view 
    // so you can physically see how large the 150m area is.
    private void OnDrawGizmosSelected()
    {
        if (vehicleTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(vehicleTransform.position, maxSpawnRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(vehicleTransform.position, minSpawnRadius);
        }
    }
}