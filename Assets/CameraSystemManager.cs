using UnityEngine;

using TMPro;



public class CameraSystemManager : MonoBehaviour

{

    public static CameraSystemManager Instance;



    [System.Serializable]

    public struct CameraNodeData

    {

        public Camera cameraComponent;

        public string cameraCustomName;

        [Tooltip("The max distance Foxy can be from this camera object to count as visible on screen.")]

        public float foxyViewDistanceThreshold;

    }



    [Header("Camera Configuration")]

    public CameraNodeData[] securityCameras;

    public int activeCameraIndex = 0;



    [Header("UI Text Display (Optional)")]

    public TextMeshProUGUI UI_CameraLabel;
    public GameObject monitorOverlay;

    [Header("Monitor Mouse Look")]
    public float mouseLookSensitivity = 2.5f;
    public float minPitch = -60f;
    public float maxPitch = 60f;

    [Header("AI Context Mapping Linkages")]

    public FoxyAI foxyAIScriptComponent;



    private bool isMonitorSystemOpen = false;



    void Awake()

    {

        if (Instance == null) Instance = this;

        else Destroy(gameObject);

    }



    void Start()

    {

        if (securityCameras == null || securityCameras.Length == 0)

        {

            Debug.LogError("CameraSystemManager: No security cameras assigned!", this);

            return;

        }

       

        activeCameraIndex = Mathf.Clamp(activeCameraIndex, 0, securityCameras.Length - 1);

        if (monitorOverlay == null)
        {
            monitorOverlay = GameObject.Find("CameraMonitorUI");
        }

        if (monitorOverlay != null)
        {
            monitorOverlay.SetActive(false);
        }

        for (int i = 0; i < securityCameras.Length; i++)

        {

            Camera cam = securityCameras[i].cameraComponent;

            if (cam != null)

            {

                cam.targetTexture = null;

                cam.depth = 10f;

                cam.enabled = false;



                if (securityCameras[i].foxyViewDistanceThreshold <= 0.1f)

                {

                    securityCameras[i].foxyViewDistanceThreshold = 15.0f;

                }

            }

        }



        if (foxyAIScriptComponent == null)

        {

            foxyAIScriptComponent = Object.FindAnyObjectByType<FoxyAI>();

        }



        UpdateCameraTargets();

    }



    void Update()

    {

        // Safety drop: if monitor closed, don't read inputs

        if (!isMonitorSystemOpen) return;



        // NO MORE INPUT SYSTEM SHORTCUTS FOR A AND D.

        // This strictly listens to the physical arrow keys on your keyboard.

        if (Input.GetKeyDown(KeyCode.RightArrow))

        {

            CycleCameraNext();

        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))

        {

            CycleCameraPrevious();

        }



        EvaluateFoxySpottedContext();

    }



    public void SetSystemActiveState(bool systemIsOn)

    {

        isMonitorSystemOpen = systemIsOn;

        Cursor.visible = systemIsOn;
        Cursor.lockState = systemIsOn ? CursorLockMode.None : CursorLockMode.Locked;

       

        if (securityCameras == null || securityCameras.Length == 0) return;



        for (int i = 0; i < securityCameras.Length; i++)

        {

            if (securityCameras[i].cameraComponent != null)

            {

                securityCameras[i].cameraComponent.enabled = false;

            }

        }



        if (monitorOverlay != null)
        {
            monitorOverlay.SetActive(systemIsOn);
        }

        if (isMonitorSystemOpen)

        {

            ToggleCurrentCameraState(true);

            UpdateCameraTargets();

        }

        else

        {

            if (foxyAIScriptComponent != null) foxyAIScriptComponent.SetVisibilityState(false);

        }

    }



    public void CycleCameraNext()

    {

        if (securityCameras == null || securityCameras.Length == 0) return;



        ToggleCurrentCameraState(false);

        activeCameraIndex = (activeCameraIndex + 1) % securityCameras.Length;

        ToggleCurrentCameraState(true);



        UpdateCameraTargets();

    }



    public void CycleCameraPrevious()

    {

        if (securityCameras == null || securityCameras.Length == 0) return;



        ToggleCurrentCameraState(false);

        activeCameraIndex--;

        if (activeCameraIndex < 0)

        {

            activeCameraIndex = securityCameras.Length - 1;

        }

        ToggleCurrentCameraState(true);



        UpdateCameraTargets();

    }



    private void ToggleCurrentCameraState(bool isOn)

    {

        if (activeCameraIndex >= 0 && activeCameraIndex < securityCameras.Length)

        {

            Camera cam = securityCameras[activeCameraIndex].cameraComponent;

            if (cam != null)

            {

                cam.enabled = isOn;

            }

        }

    }

    private void HandleMouseLook()
    {
        if (securityCameras == null || securityCameras.Length == 0) return;

        Camera cam = securityCameras[activeCameraIndex].cameraComponent;
        if (cam == null) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (Mathf.Abs(mouseX) < 0.001f && Mathf.Abs(mouseY) < 0.001f) return;

        Vector3 currentEuler = cam.transform.eulerAngles;
        currentEuler.y += mouseX * mouseLookSensitivity;
        currentEuler.x -= mouseY * mouseLookSensitivity;
        currentEuler.x = ClampAngle(currentEuler.x, minPitch, maxPitch);

        cam.transform.eulerAngles = currentEuler;
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }



    private void EvaluateFoxySpottedContext()

    {

        if (!isMonitorSystemOpen || foxyAIScriptComponent == null || securityCameras.Length == 0) return;



        Camera currentActiveCam = securityCameras[activeCameraIndex].cameraComponent;

        if (currentActiveCam == null) return;



        float distanceCamToFoxy = Vector3.Distance(currentActiveCam.transform.position, foxyAIScriptComponent.transform.position);

        float permittedThreshold = securityCameras[activeCameraIndex].foxyViewDistanceThreshold;



        if (distanceCamToFoxy <= permittedThreshold)

        {

            foxyAIScriptComponent.SetVisibilityState(true);

        }

        else

        {

            foxyAIScriptComponent.SetVisibilityState(false);

        }

    }



    private void UpdateCameraTargets()

    {

        if (securityCameras == null || securityCameras.Length == 0 || activeCameraIndex >= securityCameras.Length) return;



        if (UI_CameraLabel != null)

        {

            string customName = securityCameras[activeCameraIndex].cameraCustomName;

            UI_CameraLabel.text = string.IsNullOrEmpty(customName) ? $"CAM {activeCameraIndex + 1}" : customName;

        }



        EvaluateFoxySpottedContext();

    }

} 

