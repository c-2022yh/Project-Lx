using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class OrbLight : MonoBehaviour
{
    private Light2D light2D;

    private void Awake()
    {
        light2D = GetComponent<Light2D>();
    }

    public void SetLight(
        float intensity,
        float radius,
        Color color)
    {
        SetIntensity(intensity);
        SetRadius(radius);
        SetColor(color);
    }

    public void SetIntensity(float intensity)
    {
        light2D.intensity = Mathf.Max(0f, intensity);
    }

    public void SetRadius(float radius)
    {
        light2D.pointLightOuterRadius =
            Mathf.Max(0f, radius);
    }

    public void SetColor(Color color)
    {
        light2D.color = color;
    }
}