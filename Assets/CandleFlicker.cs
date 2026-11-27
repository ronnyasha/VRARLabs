using UnityEngine;

// Легке мерехтіння світла свічки
[RequireComponent(typeof(Light))]
public class CandleFlicker : MonoBehaviour
{
    public float amount = 0.22f;
    public float speed = 3f;

    Light l;
    float baseIntensity;
    float seed;

    void Awake()
    {
        l = GetComponent<Light>();
        baseIntensity = l.intensity;
        seed = Random.value * 100f;
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f;
        l.intensity = baseIntensity * (1f + n * amount);
    }
}
