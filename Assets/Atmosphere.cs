using UnityEngine;

public class Atmosphere : MonoBehaviour
{
    [Range(0f, 100f)]
    public float visibilityPercent = 35f;

    private void Awake()
    {
        ApplyDarkAtmosphere();
    }

    private void ApplyDarkAtmosphere()
    {
        float brightness = Mathf.Clamp01(visibilityPercent / 100f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = Mathf.Lerp(0.08f, 0.45f, 1f - brightness);
        RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.07f);
        RenderSettings.ambientLight = new Color(brightness, brightness, brightness + 0.05f);

        if (Camera.main != null)
            Camera.main.backgroundColor = RenderSettings.fogColor;
    }
}
