using UnityEngine;

public class TimeManager
{
    private readonly float[] timeScales = { 0.1f, 0.5f, 1f, 2f, 4f, 8f };

    private float gameTicksPerSecond = 5;
    public float GameTicksPerSecond
    {
        get { return gameTicksPerSecond; }
        set { gameTicksPerSecond = value; }
    }

    private int timeScaleIndex = 2;
    public int TimeScaleIndex
    {
        get{ return timeScaleIndex; }
        set
        {
            if (value > timeScales.Length || value < 0 || value == timeScaleIndex) return;
            timeScaleIndex = value;
            currentTimeScale = timeScales[value];
        }
    }

    public float GameTickDelay { get { return 1f / gameTicksPerSecond; } }
    public float DeltaTime { get; private set; }
    public float ElapsedDeltaTime { get; private set; }

    private float currentTimeScale = 1;
    public float CurrentTimeScale { get { return currentTimeScale; } }

    public void Update()
    {
        DeltaTime = Time.deltaTime * currentTimeScale;
        ElapsedDeltaTime += DeltaTime;
    }

    /// <summary>
    /// Increases the game speed by increasing the time scale by 1.
    /// </summary>
    public void IncreaseTimeScale()
    {
        TimeScaleIndex = timeScaleIndex + 1;
    }

    /// <summary>
    /// Decreases the game speed by decreasing the time scale by 1.
    /// </summary>
    public void DecreaseTimeScale()
    {
        TimeScaleIndex = timeScaleIndex - 1;
    }

    public void ResetElapsedDeltaTime()
    {
        ElapsedDeltaTime = 0;
    }
}