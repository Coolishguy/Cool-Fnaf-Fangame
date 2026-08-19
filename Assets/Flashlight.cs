using UnityEngine;

[RequireComponent(typeof(Light))]
public class Flashlight : MonoBehaviour
{
    [Header("Toggle")]
    public KeyCode toggleKey = KeyCode.F;

    [Header("Light Settings")]
    public float intensity = 500f;
    public float range = 20f;
    public float spotAngle = 45f;
    public bool startOn = false;

    private Light flashlight;

    void Awake()
    {
        flashlight = GetComponent<Light>();

        flashlight.type = LightType.Spot;
        flashlight.range = range;
        flashlight.intensity = intensity;
        flashlight.spotAngle = spotAngle;
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
