using UnityEngine;

[RequireComponent(typeof(Light))]
public class Flashlight : MonoBehaviour
{
    [Header("Toggle")]
    public KeyCode toggleKey = KeyCode.F;

    [Header("Light Settings")]
    public float intensity = 8f;
    public float range = 20f;
    public bool startOn = false;

    private Light flashlight;

    void Awake()
    {
        flashlight = GetComponent<Light>();

        flashlight.type = LightType.Point;
        flashlight.range = range;
        flashlight.intensity = intensity;
        flashlight.shadows = LightShadows.Soft;
        flashlight.renderMode = LightRenderMode.Auto;
        flashlight.enabled = startOn;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            flashlight.enabled = !flashlight.enabled;
        }
    }
}
