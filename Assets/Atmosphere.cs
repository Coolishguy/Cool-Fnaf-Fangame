using UnityEngine;

public class Atmosphere : MonoBehaviour
{
    [Range(0f, 100f)]
    public float visibilityPercent = 35f;

    [SerializeField] private bool useFog = false;

    private void Start()
    {
        ApplyDarkAtmosphere();
    }

    private void Update()
    {
        ApplyDarkAtmosphere();
    }

    private void OnValidate()
    {
        ApplyDarkAtmosphere();
    }

    private void ApplyDarkAtmosphere()
    {
        float visibility = Mathf.Clamp01(visibilityPercent / 100f);
        float darkness = 1f - visibility;

        RenderSettings.fog = useFog;
        if (useFog)
        {
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = Mathf.Lerp(0.01f, 0.18f, darkness);
            RenderSettings.fogColor = new Color(0.03f, 0.04f, 0.06f);
        }

        RenderSettings.ambientLight = new Color(
            visibility * 0.8f,
            visibility * 0.8f,
            Mathf.Clamp01((visibility * 0.8f) + 0.05f)
        );

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.Skybox;
        }
    }
}
