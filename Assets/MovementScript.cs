using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementScript : MonoBehaviour
{
    [Header("UI References")]
    // FIX: Added eatPromptText to fix CS1061 error in StaminaItem.cs
    public GameObject eatPromptText; 

    [Header("Movement Speeds")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float crouchSpeed = 2.5f;
    private float currentSpeed;

    [Header("Detection Radii for AI")]
    public float standingDetectionRadius = 25f;
    public float crouchingDetectionRadius = 10f;

    [Header("Crouch Settings")]
    public float standingHeight = 2f;
    public float crouchingHeight = 1f;
    public bool isCrouching = false;

    [Header("Stamina System")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrainRate = 15f;
    public float staminaRegenRate = 10f;
    private bool isSprinting = false;

    [Header("Keybindings")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode monitorKey = KeyCode.E;

    [Header("State Flags")]
    public bool isMonitorOpen = false;

    [Header("Mouse Look")]
    public Camera playerCamera;
    public float mouseSensitivity = 120f;
    public float minLookAngle = -70f;
    public float maxLookAngle = 80f;
    private float cameraPitch = 0f;

    private CharacterController controller;
    private Vector3 moveDirection;
    private Texture2D crosshairTexture;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentStamina = maxStamina;
        currentSpeed = walkSpeed;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera != null)
        {
            cameraPitch = playerCamera.transform.eulerAngles.x;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Hide eat prompt on game start if assigned
        if (eatPromptText != null)
        {
            eatPromptText.SetActive(false);
        }
    }

    void Update()
    {
        // 1. Handle Security Monitor Toggle
        if (Input.GetKeyDown(monitorKey))
        {
            ToggleMonitor();
        }

        // Freeze player movement if security monitor is active
        if (isMonitorOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        HandleLook();

        // 2. Handle Crouch Input
        HandleCrouch();

        // 3. Handle Sprint & Stamina
        HandleSprint();

        // 4. Apply Character Movement
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        moveDirection = move.normalized;

        controller.Move(moveDirection * currentSpeed * Time.deltaTime);
    }

    void HandleLook()
    {
        if (playerCamera == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, minLookAngle, maxLookAngle);

        playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void OnGUI()
    {
        if (isMonitorOpen) return;

        if (crosshairTexture == null)
        {
            crosshairTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            crosshairTexture.SetPixels(pixels);
            crosshairTexture.Apply();
        }

        Rect crosshairRect = new Rect(Screen.width / 2f - 2f, Screen.height / 2f - 2f, 4f, 4f);
        GUI.DrawTexture(crosshairRect, crosshairTexture);
    }

    void HandleCrouch()
    {
        isCrouching = Input.GetKey(crouchKey);

        if (isCrouching)
        {
            controller.height = crouchingHeight;
            currentSpeed = crouchSpeed;
        }
        else
        {
            controller.height = standingHeight;
            currentSpeed = (Input.GetKey(sprintKey) && currentStamina > 0) ? sprintSpeed : walkSpeed;
        }
    }

    void HandleSprint()
    {
        bool isMoving = moveDirection.magnitude > 0.1f;

        if (isCrouching)
        {
            isSprinting = false;
            currentSpeed = crouchSpeed;
            return;
        }

        if (Input.GetKey(sprintKey) && isMoving && currentStamina > 0f)
        {
            isSprinting = true;
            currentSpeed = sprintSpeed;
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        }
        else
        {
            isSprinting = false;
            currentSpeed = walkSpeed;

            // Regenerate stamina when not sprinting
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            }
        }
    }

    void ToggleMonitor()
    {
        isMonitorOpen = !isMonitorOpen;

        if (isMonitorOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (CameraSystemManager.Instance != null)
        {
            CameraSystemManager.Instance.SetSystemActiveState(isMonitorOpen);
        }
        else
        {
            Debug.LogWarning("CameraSystemManager Instance not found in scene!");
        }
    }
}