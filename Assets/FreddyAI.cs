using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class FreddyAI : MonoBehaviour, IAnimatronicDifficulty
{
    [Header("Target & Detection Settings")]
    public MovementScript playerMovement;
    public float catchDistance = 1.8f;
    public float checkRate = 0.2f;
    public float baseCheckRate;
    private float checkTimer = 0f;

    [Header("AI State Checks")]
    public bool isChasing = false;
    public float idleSpeed = 1.8f;
    public float chaseSpeed = 4.2f;
    public float baseChaseSpeed;

    [Header("Difficulty Scaling")]
    public float difficultyMultiplier = 1f;

    [Header("Stage Fall Settings")]
    public bool startOnStage = true;
    public float stageFallDelay = 3f;
    public float stageFallSpeed = 2.5f;
    public float stageFloorY = 0f;
    private float stageFallTimer = 0f;
    private bool hasFallenFromStage = false;
    private bool isGameOver = false;
    private float stageStartY;

    private NavMeshAgent agent;
    private AudioSource audioSource;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        stageStartY = transform.position.y;
        baseCheckRate = checkRate;
        baseChaseSpeed = chaseSpeed;

        if (agent != null)
        {
            agent.speed = idleSpeed;
            agent.acceleration = 8f;
            agent.angularSpeed = 180f;
        }

        if (playerMovement == null)
        {
            playerMovement = Object.FindAnyObjectByType<MovementScript>();
        }
    }

    public void ApplyDifficulty(float aggressionMultiplier, float timeProgress, float wildnessMeter, int difficultyTier)
    {
        difficultyMultiplier = aggressionMultiplier;
        checkRate = Mathf.Clamp(baseCheckRate / aggressionMultiplier, 0.08f, 0.5f);
        chaseSpeed = baseChaseSpeed * Mathf.Clamp(0.8f + (aggressionMultiplier * 0.45f), 0.75f, 2.2f);
    }

    void Update()
    {
        if (isGameOver) return;

        if (startOnStage && !hasFallenFromStage)
        {
            stageFallTimer += Time.deltaTime;

            if (stageFallTimer >= stageFallDelay)
            {
                FallFromStage();
            }

            return;
        }

        checkTimer += Time.deltaTime;

        if (checkTimer >= checkRate)
        {
            ExecuteDetectionCheck();
            checkTimer = 0f;
        }

        if (isChasing)
        {
            if (agent != null)
            {
                agent.speed = chaseSpeed;
            }

            ChasePlayer();
        }
        else
        {
            if (agent != null)
            {
                agent.speed = idleSpeed;
            }

            if (agent != null && agent.isOnNavMesh && !agent.isStopped)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
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

    void ExecuteDetectionCheck()
    {
        if (playerMovement == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerMovement.transform.position);

        float activeDetectionRadius = playerMovement.isCrouching
            ? playerMovement.crouchingDetectionRadius
            : playerMovement.standingDetectionRadius;

        float adjustedDetectionRadius = activeDetectionRadius * difficultyMultiplier;
        isChasing = distanceToPlayer <= adjustedDetectionRadius;
    }

    void ChasePlayer()
    {
        if (agent == null || playerMovement == null || !agent.isOnNavMesh) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerMovement.transform.position);

        if (distanceToPlayer <= catchDistance)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            TriggerJumpscare();
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(playerMovement.transform.position);
        }
    }

    void TriggerJumpscare()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }

        Debug.LogWarning("FREDDY JUMPSCARED YOU! GAME OVER.");

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
