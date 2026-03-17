using UnityEngine;

public class RadioNeedleDisplay : MonoBehaviour
{
    [Header("Dependencies")]
    public RadioTuner tuner;

    [Header("Frequency Match")]
    public float minFrequency = 88.0f;
    public float maxFrequency = 108.0f;

    [Header("Slide Positions")]
    public Vector3 posAtMinFrequency;
    public Vector3 posAtMaxFrequency;

    [Header("Texture Correction")]
    [Tooltip("Bends the math to match imperfectly drawn textures. Default is a straight line from (0,0) to (1,1).")]
    public AnimationCurve positionCorrection = AnimationCurve.Linear(0, 0, 1, 1);

    void Update()
    {
        if (tuner == null) return;

        // 1. Get the pure mathematical percentage (e.g., 100 MHz is exactly 60% or 0.6)
        float rawPercent = Mathf.InverseLerp(minFrequency, maxFrequency, tuner.currentFrequency);

        // 2. Pass that percentage through your custom curve to fix the texture alignment
        float correctedPercent = positionCorrection.Evaluate(rawPercent);

        // 3. Move the needle!
        transform.localPosition = Vector3.Lerp(posAtMinFrequency, posAtMaxFrequency, correctedPercent);
    }
}