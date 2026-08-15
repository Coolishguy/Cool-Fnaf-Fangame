// StaminaItem.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class StaminaItem : MonoBehaviour
{
    [Header("Donut Physics Settings")]
    public float staminaRestoreAmount = 83.33f; // Restores exactly 25 seconds of sprint time

    [Header("Radius Configuration")]
    [Tooltip("How close Michael's camera needs to be (in meters) for the text to appear.")]
    public float interactionRadius = 8.0f; 

    [Header("Dumbo Animation Settings")]
    public float pulseSpeed = 6f;     // How fast the text expands and shrinks
    public float pulseAmount = 0.15f;   // How dramatically it changes size
    public float wiggleSpeed = 8f;     // How fast it shakes side to side
    public float wiggleAmount = 15f;    // How far it shakes side to side

    private MovementScript playerMovement;
    private Transform cameraTransform;
    private bool isTextActive = false;
    private RectTransform textRectTransform;
    private Vector3 originalTextPosition;

    // STATIC GLOBAL TRACKER: Tracks which specific donut currently owns the shared UI
    private static StaminaItem currentUIOwner = null;

    void Start()
    {
        playerMovement = Object.FindAnyObjectByType<MovementScript>();
        
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (playerMovement == null)
        {
            Debug.LogError("CRITICAL ERROR: Could not find Michael Afton's MovementScript in the scene!");
        }
        else if (playerMovement.eatPromptText != null)
        {
            textRectTransform = playerMovement.eatPromptText.GetComponent<RectTransform>();
            originalTextPosition = textRectTransform.anchoredPosition;
        }
    }

    void Update()
    {
        if (playerMovement == null || cameraTransform == null) return;

        float distanceToCamera = Vector3.Distance(transform.position, cameraTransform.position);

        if (distanceToCamera <= interactionRadius)
        {
            // PRIORITY CHECK: Only take control if no one else is using the UI, or if WE are already using it
            if (currentUIOwner == null || currentUIOwner == this)
            {
                currentUIOwner = this;

                if (!isTextActive && playerMovement.eatPromptText != null)
                {
                    playerMovement.eatPromptText.SetActive(true);
                    isTextActive = true;
                }

                // DUMBO ANIMATION ROUTINE
                if (isTextActive && textRectTransform != null)
                {
                    float scaleMod = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                    textRectTransform.localScale = new Vector3(scaleMod, scaleMod, 1f);

                    float xOffset = Mathf.Cos(Time.time * wiggleSpeed) * wiggleAmount;
                    textRectTransform.anchoredPosition = new Vector3(originalTextPosition.x + xOffset, originalTextPosition.y, 0f);
                }

                // INTERACTION ENGINE
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    EatDonut();
                }
            }
        }
        else
        {
            // Only turn off the text if THIS specific donut was the one using it
            if (currentUIOwner == this)
            {
                ClearUIOwnership();
            }
        }
    }

    void EatDonut()
    {
        playerMovement.currentStamina += staminaRestoreAmount;
        playerMovement.currentStamina = Mathf.Min(playerMovement.currentStamina, playerMovement.maxStamina);
        
        if (currentUIOwner == this)
        {
            ClearUIOwnership();
        }
        
        Destroy(gameObject);
    }

    void ClearUIOwnership()
    {
        if (playerMovement != null && playerMovement.eatPromptText != null)
        {
            ResetTextTransforms();
            playerMovement.eatPromptText.SetActive(false);
        }
        isTextActive = false;
        currentUIOwner = null; // Free up the UI for other donuts in the scene
    }

    void ResetTextTransforms()
    {
        if (textRectTransform != null)
        {
            textRectTransform.localScale = Vector3.one;
            textRectTransform.anchoredPosition = originalTextPosition;
        }
    }

    // Safety fallback: If a donut is destroyed externally, clear ownership so UI isn't permanently locked
    void OnDestroy()
    {
        if (currentUIOwner == this)
        {
            ClearUIOwnership();
        }
    }
}
