using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class FoxyAI : MonoBehaviour, IAnimatronicDifficulty
{
    [Header("Target & Tracking Configuration")]
    public MovementScript playerMovement;
    public FreddyAI freddyAI;

    [Header("Foxy Attack Metrics")]
    public float catchDistance = 2.0f;
    public float baseDetectionRadius = 40f;
    public float currentDetectionRadius;
    public float idleSpeed = 1.8f;
    public float chaseSpeed = 5.2f;
    public float baseChaseSpeed;
    public float difficultyMultiplier = 1f;

    [Header("Stage Fall Settings")]
    public bool startOnStage = true;
    public float stageFallDelay = 3f;
    public float stageFallSpeed = 2.5f;
    public float stageFloorY = 0f;
    private float stageFallTimer = 0f;
    private bool hasFallenFromStage = false;
    private float stageStartY;

    [Header("Quick-Time Click Mechanics")]
    public int currentRequiredClicks;
    public float currentActionTimeWindow;
    private int clickCounter = 0;
    private float defenseTimer = 0f;

    public bool isSpottedByPlayer = false;
    private bool isDefending = false;
    private bool hasFailedDefend = false;
    private float reTriggerCooldown = 0f;

    private NavMeshAgent agent;
    private AudioSource audioSource;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        currentDetectionRadius = baseDetectionRadius;
        baseChaseSpeed = chaseSpeed;
        stageStartY = transform.position.y;

        if (agent != null)
        {
            agent.speed = idleSpeed;
            agent.acceleration = 9f;
            agent.angularSpeed = 220f;
        }

        if (playerMovement == null)
        {
            playerMovement = Object.FindAnyObjectByType<MovementScript>();
        }
    }

    public void ApplyDifficulty(float aggressionMultiplier, float timeProgress, float wildnessMeter, int difficultyTier)
    {
        difficultyMultiplier = aggressionMultiplier;
        currentDetectionRadius = baseDetectionRadius * Mathf.Clamp(aggressionMultiplier, 0.7f, 1.9f);
        chaseSpeed = baseChaseSpeed * Mathf.Clamp(0.8f + (aggressionMultiplier * 0.45f), 0.75f, 2.3f);
    }

    void Update()
    {
        if (startOnStage && !hasFallenFromStage)
        {
            stageFallTimer += Time.deltaTime;

            if (stageFallTimer >= stageFallDelay)
            {
                FallFromStage();
            }

            return;
        }

        if (reTriggerCooldown > 0f)
        {
            reTriggerCooldown -= Time.deltaTime;
        }

        if (hasFailedDefend)
        {
            if (playerMovement != null)
            {
                ChasePlayer(Vector3.Distance(transform.position, playerMovement.transform.position));
            }
            return;
        }

        if (isSpottedByPlayer && !isDefending && reTriggerCooldown <= 0f)
        {
            StartDefenseGame();
        }
        else if (!isDefending && Input.GetMouseButtonDown(0) && reTriggerCooldown <= 0f)
        {
            float rngChance = Random.Range(0f, 100f);
            if (rngChance <= 5f)
            {
                Debug.Log("[FOXY BLIND TRIGGER] Lucky blind click hit the 5% chance!");
                StartDefenseGame();
            }
        }

        if (isDefending)
        {
            ProcessDefenseGame();
            return;
        }

        if (playerMovement != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerMovement.transform.position);

            if (distanceToPlayer <= currentDetectionRadius)
            {
                isSpottedByPlayer = true;
                if (agent != null)
                {
                    agent.speed = chaseSpeed;
                }
                ChasePlayer(distanceToPlayer);
            }
            else
            {
                isSpottedByPlayer = false;
                if (agent != null)
                {
                    agent.speed = idleSpeed;
                }

                if (agent != null && !agent.isStopped)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
            }
        }
    }

    void FallFromStage()
    {
        if (hasFallenFromStage) return;

        if (agent != null)
        {
            agent.enabled = false;
        }

        Vector3 fallPosition = transform.position;
        fallPosition.y = Mathf.Max(stageFloorY, fallPosition.y - stageFallSpeed * Time.deltaTime);
        transform.position = fallPosition;

        if (transform.position.y <= stageFloorY)
        {
            Vector3 landedPosition = transform.position;
            landedPosition.y = stageFloorY;
            transform.position = landedPosition;

            hasFallenFromStage = true;

            if (agent != null)
            {
                agent.enabled = true;
                agent.Warp(transform.position);
                agent.isStopped = false;
            }
        }
    }

    void StartDefenseGame()
    {
        currentRequiredClicks = Random.Range(15, 21);
        currentActionTimeWindow = Random.Range(15f, 21f);
        clickCounter = 0;
        defenseTimer = 0f;
        isDefending = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        Debug.Log($"[FOXY SEEN / BLIND TRIGGERED] Click {currentRequiredClicks} times in {currentActionTimeWindow:F1}s!");
    }

    void ProcessDefenseGame()
    {
        defenseTimer += Time.deltaTime;

        if (Input.GetMouseButtonDown(0))
        {
            clickCounter++;
            Debug.Log($"Foxy Hit! Clicks: {clickCounter}/{currentRequiredClicks}");
        }

        if (clickCounter >= currentRequiredClicks && defenseTimer <= currentActionTimeWindow)
        {
            FoxyGoesAway();
        }
        else if (defenseTimer > currentActionTimeWindow && !hasFailedDefend)
        {
            ApplyFailurePenalty();
        }
    }

    void ChasePlayer(float distance)
    {
        if (agent == null || playerMovement == null) return;

        if (distance <= catchDistance)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            KillPlayerSequence();
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(playerMovement.transform.position);
        }
    }

    void FoxyGoesAway()
    {
        Debug.Log("Foxy was stalled successfully!");
        isSpottedByPlayer = false;
        isDefending = false;
        clickCounter = 0;
        defenseTimer = 0f;
        reTriggerCooldown = 5.0f;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    void ApplyFailurePenalty()
    {
        hasFailedDefend = true;
        isDefending = false;

        currentDetectionRadius *= 1.5f;

        if (playerMovement != null)
        {
            playerMovement.standingDetectionRadius *= 1.5f;
            playerMovement.crouchingDetectionRadius *= 1.5f;
        }

        if (freddyAI != null)
        {
            freddyAI.checkRate = 0.05f;
        }

        Debug.LogError($"[⚠️ CONTAINMENT BREACH] Foxy broke containment! Detection parameters permanently tracking 1.5x wider!");
    }

    void KillPlayerSequence()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }

        Debug.LogWarning("FOXY JUMPSCARED YOU! GAME OVER.");

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SetVisibilityState(bool seen)
    {
        isSpottedByPlayer = seen;
    }
}