using UnityEngine;

public class SnowWindEffect : MonoBehaviour
{
    private ParticleSystem ps;
    private ParticleSystem.VelocityOverLifetimeModule velocity;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
    }

    void Update()
    {
        Vector3 wind = WindController.Instance.GetWindForce();

        velocity.x = new ParticleSystem.MinMaxCurve(wind.x);
        velocity.y = new ParticleSystem.MinMaxCurve(wind.y);
        velocity.z = new ParticleSystem.MinMaxCurve(wind.z);
    }
}