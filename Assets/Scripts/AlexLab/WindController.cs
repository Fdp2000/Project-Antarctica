using UnityEngine;

public class WindController : MonoBehaviour
{
    public static WindController Instance;

    public Vector3 windDirection = new Vector3(1f, 0f, 0f);
    public float windStrength = 5f;

    Vector3 targetDirection;
    float targetStrength;

    public float directionChangeInterval = 5f;
    float timer;

    void Awake()
    {
        Instance = this;

        targetDirection = windDirection;
        targetStrength = windStrength;
    }

    public Vector3 GetWindForce()
    {
        return windDirection.normalized * windStrength;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Every few seconds pick a new wind target
        if (timer > directionChangeInterval)
        {
            timer = 0;

           targetDirection = Quaternion.Euler(
            0,
             Random.Range(-40f, 40f),
            0
            ) * windDirection;

            targetStrength = Random.Range(3f, 10f);
        }

        // Smoothly move toward target wind
        windDirection = Vector3.Lerp(windDirection, targetDirection, Time.deltaTime * 0.5f);
        windStrength = Mathf.Lerp(windStrength, targetStrength, Time.deltaTime * 0.5f);
    }
}