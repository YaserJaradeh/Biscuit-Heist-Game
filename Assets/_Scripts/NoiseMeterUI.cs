using UnityEngine;
using UnityEngine.UI;

public class NoiseMeterUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Slider slider;         // assign in Inspector
    [SerializeField] private Image fillImage;       // assign Fill Image (for color tweaks)

    [Header("Tuning")]
    [SerializeField, Range(0f, 1f)] private float target; // live noise 0..1
    [SerializeField] private float smooth = 8f;     // fill smoothing
    [SerializeField] private Gradient colorRamp;    // green->yellow->red
    
    [Header("Noise Fluctuation")]
    [SerializeField, Range(0f, 0.1f)] private float fluctuationRange = 0.02f; // how much the noise varies
    [SerializeField] private float fluctuationSpeed = 5f; // how fast the noise changes
    
    [Header("Pulse Effect")]
    [SerializeField, Range(0f, 1f)] private float pulseThreshold = 0.85f; // noise level to start pulsing
    [SerializeField] private float pulseSpeed = 8f; // how fast the pulse effect
    [SerializeField, Range(0f, 1f)] private float pulseAmplitude = 0.2f; // strength of pulse effect

    private float current;
    private float noiseOffset;

    void Reset() {
        slider = GetComponent<Slider>();
        if (slider != null && slider.fillRect != null)
            fillImage = slider.fillRect.GetComponent<Image>();
    }

    void Update()
    {
        // Generate realistic noise fluctuation using Perlin noise
        noiseOffset += Time.deltaTime * fluctuationSpeed;
        float noise = Mathf.PerlinNoise(noiseOffset, 0f) * 2f - 1f; // range -1 to 1
        float fluctuatedTarget = target + noise * fluctuationRange;
        
        // Smoothly approach the fluctuated target value
        current = Mathf.Lerp(current, Mathf.Clamp01(fluctuatedTarget), 1f - Mathf.Exp(-smooth * Time.deltaTime));
        slider.value = current;

        if (fillImage != null && colorRamp != null)
            fillImage.color = colorRamp.Evaluate(current);

        PulseIfLoud(pulseThreshold, pulseSpeed, pulseAmplitude);
    }

    // Call this from your gameplay systems to update the meter
    public void SetNoise01(float value) => target = Mathf.Clamp01(value);

    // Optional: flash when over threshold
    public void PulseIfLoud(float threshold = 0.85f, float speed = 8f, float amplitude = 0.2f)
    {
        if (current >= threshold && fillImage != null)
        {
            float a = 1f - amplitude * 0.5f + Mathf.Abs(Mathf.Sin(Time.time * speed)) * amplitude;
            var c = fillImage.color; c.a = a; fillImage.color = c;
        }
    }
}