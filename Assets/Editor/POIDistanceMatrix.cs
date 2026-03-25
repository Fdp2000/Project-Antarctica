using UnityEngine;
using UnityEditor;

public class POIDistanceMatrix : EditorWindow
{
    private Vector2 scrollPosition;
    private float warningDistance = 400f;

    [MenuItem("Tools/Antarctica/POI Distance Calculator")]
    public static void ShowWindow()
    {
        GetWindow<POIDistanceMatrix>("POI Distances");
    }

    void OnGUI()
    {
        GUILayout.Label("Radio Beacon Distance Matrix", EditorStyles.boldLabel);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Warn if closer than (meters):", GUILayout.Width(170));
        warningDistance = EditorGUILayout.FloatField(warningDistance, GUILayout.Width(50));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (GUILayout.Button("Calculate Distances Now", GUILayout.Height(30)))
        {
            Repaint();
        }

        GUILayout.Space(10);

        RadioBeacon[] allBeacons = FindObjectsOfType<RadioBeacon>();

        if (allBeacons.Length == 0)
        {
            EditorGUILayout.HelpBox("No RadioBeacons found in the scene!", MessageType.Warning);
            return;
        }

        // --- NEW: Find the vehicle to calculate drive time! ---
        VehicleController vehicle = FindObjectOfType<VehicleController>();
        float vehicleSpeed = vehicle != null ? vehicle.maxSpeed : 5.0f;

        if (vehicle != null)
        {
            EditorGUILayout.HelpBox($"Vehicle found. Calculating drive times at max speed: {vehicleSpeed} m/s.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("No VehicleController found. Defaulting drive calculations to 5 m/s.", MessageType.Info);
        }

        GUILayout.Space(10);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < allBeacons.Length; i++)
        {
            RadioBeacon source = allBeacons[i];

            GUILayout.Label($"From: {source.gameObject.name}", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            for (int j = 0; j < allBeacons.Length; j++)
            {
                if (i == j) continue;

                RadioBeacon target = allBeacons[j];
                float distance = Vector3.Distance(source.transform.position, target.transform.position);

                // --- NEW: Time Calculation ---
                float timeInSeconds = distance / vehicleSpeed;
                int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
                int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
                string timeText = $"{minutes}m {seconds}s";

                // --- THE UI FIX: Using a single string to prevent column clipping ---
                if (distance < warningDistance)
                {
                    GUI.contentColor = new Color(1f, 0.4f, 0.4f);
                    GUILayout.Label($"[WARNING] To {target.gameObject.name}: {distance:F0}m | Est. Drive: {timeText} (OVERLAP RISK)");
                    GUI.contentColor = Color.white;
                }
                else
                {
                    GUILayout.Label($"To {target.gameObject.name}: {distance:F0}m | Est. Drive: {timeText}");
                }
            }

            EditorGUI.indentLevel--;
            GUILayout.Space(15);
        }

        GUILayout.EndScrollView();
    }
}