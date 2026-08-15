using UnityEngine;
using System.Collections.Generic;

public interface IAnimatronicDifficulty
{
    void ApplyDifficulty(float aggressionMultiplier, float timeProgress, float wildnessMeter, int difficultyTier);
}

public class DynamicDifficulty : MonoBehaviour
{
    public enum Playstyle
    {
        Safe,
        Balanced,
        Wild
    }

    [Header("Night Timer")]
    [SerializeField] private float totalNightLength = 420f; // 7 minutes
    [SerializeField] private float segmentLength = 70f; // 1 minute 10 seconds per difficulty phase
    [SerializeField] private bool timerRunning = true;

    [Header("Player Behavior")]
    [SerializeField] private MovementScript player;
    [SerializeField] private float safeDistanceThreshold = 15f;
    [SerializeField] private float wildDistanceThreshold = 6f;
    [SerializeField] private float behaviorUpdateInterval = 0.25f;

    public float elapsedTime { get; private set; }
    public float aggressionMultiplier { get; private set; } = 1f;
    public float wildnessMeter { get; private set; } = 0.5f;
    public Playstyle currentPlaystyle { get; private set; } = Playstyle.Balanced;
    public int difficultyTier { get; private set; }

    private float behaviorTimer;
    private bool nightEnded;

    private void Start()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<MovementScript>();
        }

        behaviorTimer = behaviorUpdateInterval;
        ApplyDifficultyToAnimatronics();
    }

    private void Update()
    {
        if (timerRunning)
        {
            elapsedTime += Time.deltaTime;

            if (elapsedTime >= totalNightLength)
            {
                elapsedTime = totalNightLength;
                timerRunning = false;
                EndNight();
            }
        }

        behaviorTimer += Time.deltaTime;
        if (behaviorTimer >= behaviorUpdateInterval)
        {
            DeterminePlaystyle();
            behaviorTimer = 0f;
        }

        UpdateDifficulty();
        ApplyDifficultyToAnimatronics();
    }

    private void EndNight()
    {
        if (nightEnded)
        {
            return;
        }

        nightEnded = true;
        Time.timeScale = 0f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void UpdateDifficulty()
    {
        float timeProgress = Mathf.Clamp01(elapsedTime / totalNightLength);
        difficultyTier = Mathf.Clamp(Mathf.FloorToInt(elapsedTime / segmentLength), 0, 6);

        float timePressure = timeProgress * 0.75f;

        float playstyleBias;
        if (currentPlaystyle == Playstyle.Safe)
        {
            playstyleBias = 0.28f;
        }
        else if (currentPlaystyle == Playstyle.Wild)
        {
            playstyleBias = -0.22f;
        }
        else
        {
            playstyleBias = 0f;
        }

        aggressionMultiplier = 1f + timePressure + playstyleBias;
        aggressionMultiplier = Mathf.Clamp(aggressionMultiplier, 0.7f, 1.9f);
    }

    private void DeterminePlaystyle()
    {
        if (player == null)
        {
            wildnessMeter = 0.5f;
            currentPlaystyle = Playstyle.Balanced;
            return;
        }

        float closestDistance = GetClosestDistanceToAnimatronic();

        if (closestDistance == Mathf.Infinity)
        {
            wildnessMeter -= 0.12f;
        }
        else if (closestDistance <= wildDistanceThreshold)
        {
            wildnessMeter += 0.18f;
        }
        else if (closestDistance >= safeDistanceThreshold)
        {
            wildnessMeter -= 0.14f;
        }
        else
        {
            wildnessMeter -= 0.04f;
        }

        wildnessMeter = Mathf.Clamp01(wildnessMeter);

        if (wildnessMeter < 0.35f)
        {
            currentPlaystyle = Playstyle.Safe;
        }
        else if (wildnessMeter > 0.7f)
        {
            currentPlaystyle = Playstyle.Wild;
        }
        else
        {
            currentPlaystyle = Playstyle.Balanced;
        }
    }

    private float GetClosestDistanceToAnimatronic()
    {
        float closestDistance = Mathf.Infinity;

        foreach (var animatronic in FindAnimatronics())
        {
            if (animatronic == null || player == null)
            {
                continue;
            }

            float distance = Vector3.Distance(animatronic.transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
            }
        }

        return closestDistance;
    }

    private List<MonoBehaviour> FindAnimatronics()
    {
        var animatronics = new List<MonoBehaviour>();

        foreach (var obj in Object.FindObjectsByType<MonoBehaviour>())
        {
            if (obj is IAnimatronicDifficulty)
            {
                animatronics.Add(obj);
            }
        }

        return animatronics;
    }

    private void ApplyDifficultyToAnimatronics()
    {
        foreach (var animatronic in FindAnimatronics())
        {
            if (animatronic is IAnimatronicDifficulty difficultyAnimatronic)
            {
                difficultyAnimatronic.ApplyDifficulty(
                    aggressionMultiplier,
                    Mathf.Clamp01(elapsedTime / totalNightLength),
                    wildnessMeter,
                    difficultyTier);
            }
        }
    }

    public bool IsNightComplete()
    {
        return elapsedTime >= totalNightLength;
    }
}
